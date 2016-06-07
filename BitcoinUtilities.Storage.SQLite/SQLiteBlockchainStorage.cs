using System;
using System.Collections.Generic;
using System.Data.SQLite;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Transactions;
using NLog;

namespace BitcoinUtilities.Storage.SQLite
{
    public class SQLiteBlockchainStorage : IBlockchainStorage, IDisposable
    {
        private static readonly Logger logger = LogManager.GetCurrentClassLogger();

        private const string BlockchainFilename = "blockchain.db";
        private const string BlocksFilename = "blocks.db";

        private readonly string storageLocation;
        private readonly SQLiteConnection conn;

        private SQLiteBlockchainStorage(string storageLocation, SQLiteConnection conn)
        {
            this.storageLocation = storageLocation;
            this.conn = conn;
        }

        public void Dispose()
        {
            conn.Close();
            conn.Dispose();
        }

        public static SQLiteBlockchainStorage Open(string storageLocation)
        {
            if (!Directory.Exists(storageLocation))
            {
                Directory.CreateDirectory(storageLocation);
            }

            SQLiteConnectionStringBuilder connStrBuilder = new SQLiteConnectionStringBuilder();
            connStrBuilder.DataSource = Path.Combine(storageLocation, BlockchainFilename);
            connStrBuilder.JournalMode = SQLiteJournalModeEnum.Wal;

            string connStr = connStrBuilder.ConnectionString;

            SQLiteConnection conn = new SQLiteConnection(connStr);
            conn.Open();

            ApplySchemaSettings(conn, "main");

            //todo: SQLite in WAL mode does not guarantee atomic commits across all attached databases
            AttachDatabase(conn, "blocks", Path.Combine(storageLocation, BlocksFilename));

            ApplySchemaSettings(conn, "blocks");

            //todo: handle exceptions, describe ant test them
            CheckSchema(conn);

            return new SQLiteBlockchainStorage(storageLocation, conn);
        }

        private static void AttachDatabase(SQLiteConnection conn, string schema, string path)
        {
            string escapedPath = path.Replace("'", "''");
            ExecuteSql(conn, $"attach '{escapedPath}' as {schema}");
        }

        private static void ApplySchemaSettings(SQLiteConnection conn, string schema)
        {
            ExecuteSql(conn, $"PRAGMA {schema}.journal_mode=wal");
            ExecuteSql(conn, $"PRAGMA {schema}.synchronous=0");
            ExecuteSql(conn, $"PRAGMA {schema}.cache_size=32000");
            ExecuteSql(conn, $"PRAGMA {schema}.wal_autocheckpoint=8192");
            ExecuteSql(conn, $"PRAGMA {schema}.locking_mode=EXCLUSIVE");
            ExecuteSql(conn, $"PRAGMA {schema}.foreign_keys=ON");
        }

        private static void CheckSchema(SQLiteConnection conn)
        {
            using (var tx = conn.BeginTransaction())
            {
                long hasTable;

                using (SQLiteCommand command = new SQLiteCommand(
                    "select count(*) from sqlite_master WHERE type='table' AND name='Blocks'", conn))
                {
                    hasTable = (long) command.ExecuteScalar();
                }

                if (hasTable == 0)
                {
                    CreateSchema(conn);
                }

                tx.Commit();
            }
        }

        private static void CreateSchema(SQLiteConnection conn)
        {
            //todo: move this method to repository?

            string createSchemaSql;
            Assembly schemaAssembly = typeof(SQLiteBlockchainStorage).Assembly;
            string schemaResourceName = $"{typeof(SQLiteBlockchainStorage).Namespace}.Sql.create.sql";
            using (var stream = schemaAssembly.GetManifestResourceStream(schemaResourceName))
            {
                using (StreamReader reader = new StreamReader(stream, Encoding.UTF8))
                {
                    createSchemaSql = reader.ReadToEnd();
                }
            }

            using (SQLiteCommand command = new SQLiteCommand(createSchemaSql, conn))
            {
                command.ExecuteNonQuery();
            }
        }

        //todo: move to utils?
        private static void ExecuteSql(SQLiteConnection conn, string sql)
        {
            using (SQLiteCommand command = new SQLiteCommand(sql, conn))
            {
                command.ExecuteNonQuery();
            }
        }

        public BlockLocator GetCurrentChainLocator()
        {
            List<StoredBlock> headers = new List<StoredBlock>();

            using (BlockchainRepository repo = new BlockchainRepository(conn))
            {
                using (var tx = conn.BeginTransaction())
                {
                    StoredBlock lastBlock = repo.GetLastBlockHeader();
                    if (lastBlock != null)
                    {
                        headers = repo.ReadHeadersWithHeight(BlockLocator.GetRequiredBlockHeights(lastBlock.Height));
                    }
                    tx.Commit();
                }
            }

            BlockLocator locator = new BlockLocator();

            foreach (StoredBlock header in headers)
            {
                locator.AddHash(header.Height, header.Hash);
            }

            return locator;
        }

        public StoredBlock FindBlockByHash(byte[] hash)
        {
            using (BlockchainRepository repo = new BlockchainRepository(conn))
            {
                using (var tx = conn.BeginTransaction())
                {
                    StoredBlock block = repo.FindBlockByHash(hash);

                    //todo: is it worth commiting?
                    tx.Commit();

                    return block;
                }
            }
        }

        public StoredBlock FindBestHeaderChain()
        {
            using (BlockchainRepository repo = new BlockchainRepository(conn))
            {
                using (var tx = conn.BeginTransaction())
                {
                    StoredBlock block = repo.FindBestHeaderChain();

                    //todo: is it worth commiting?
                    tx.Commit();

                    return block;
                }
            }
        }

        public List<StoredBlock> GetOldestBlocksWithoutContent(int maxCount)
        {
            using (BlockchainRepository repo = new BlockchainRepository(conn))
            {
                using (var tx = conn.BeginTransaction())
                {
                    List<StoredBlock> blocks = repo.GetOldestBlocksWithoutContent(maxCount);

                    //todo: is it worth commiting?
                    tx.Commit();

                    return blocks;
                }
            }
        }

        public List<StoredBlock> GetBlocksByHeight(int[] heights)
        {
            using (BlockchainRepository repo = new BlockchainRepository(conn))
            {
                using (var tx = conn.BeginTransaction())
                {
                    List<StoredBlock> blocks = repo.ReadHeadersWithHeight(heights);

                    //todo: is it worth commiting?
                    tx.Commit();

                    return blocks;
                }
            }
        }

        public Subchain FindSubchain(byte[] hash, int length)
        {
            using (BlockchainRepository repo = new BlockchainRepository(conn))
            {
                using (var tx = conn.BeginTransaction())
                {
                    StoredBlock block = repo.FindBlockByHash(hash);

                    if (block == null)
                    {
                        return null;
                    }

                    List<StoredBlock> blocks = new List<StoredBlock>();
                    blocks.Add(block);

                    for (int i = 1; i < length; i++)
                    {
                        if (block.Header.IsFirst)
                        {
                            //genesis block is reached;
                            break;
                        }

                        block = repo.FindBlockByHash(block.Header.PrevBlock);

                        if (block == null)
                        {
                            throw new InvalidOperationException("The storage has a chain that starts with an invalid genesis block.");
                        }

                        blocks.Add(block);
                    }

                    //todo: is it worth commiting?
                    tx.Commit();

                    blocks.Reverse();

                    return new Subchain(blocks);
                }
            }
        }

        public StoredBlock FindBlockByHeight(int height)
        {
            using (BlockchainRepository repo = new BlockchainRepository(conn))
            {
                using (var tx = conn.BeginTransaction())
                {
                    //todo: rewrite this method
                    List<StoredBlock> blocks = repo.ReadHeadersWithHeight(new int[] {height});

                    if (!blocks.Any())
                    {
                        return null;
                    }

                    //todo: is it worth commiting?
                    tx.Commit();

                    return blocks[0];
                }
            }
        }

        public void AddBlock(StoredBlock block)
        {
            JoinCurrentTransaction();
            using (BlockchainRepository repo = new BlockchainRepository(conn))
            {
                repo.InsertBlock(block);
            }
        }

        public void UpdateBlock(StoredBlock block)
        {
            JoinCurrentTransaction();
            using (BlockchainRepository repo = new BlockchainRepository(conn))
            {
                repo.UpdateBlock(block);
            }
        }

        public void AddBlockContent(byte[] hash, byte[] content)
        {
            JoinCurrentTransaction();
            using (BlockchainRepository repo = new BlockchainRepository(conn))
            {
                //todo: what if there is no such block or it already has content ?
                repo.AddBlockContent(hash, content);
            }
        }

        private void JoinCurrentTransaction()
        {
            //todo: add a test to check that storage respects transaction
            Transaction tx = Transaction.Current;
            if (tx != null)
            {
                conn.EnlistTransaction(tx);
            }
        }
    }
}