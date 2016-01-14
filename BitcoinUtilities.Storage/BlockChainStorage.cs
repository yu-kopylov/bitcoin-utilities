using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SQLite;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using BitcoinUtilities.Storage.Converters;
using BitcoinUtilities.Storage.Models;
using NLog;

namespace BitcoinUtilities.Storage
{
    public class BlockChainStorage : IDisposable
    {
        private static readonly Logger logger = LogManager.GetCurrentClassLogger();

        private const string BlockchainFilename = "blockchain.db";
        private const string TransactionHashesFilename = "txhash.db";
        private const string BinaryDataFilename = "binary.db";
        private const string AddressesFilename = "addr.db";

        private readonly string storageLocation;
        private readonly SQLiteConnection conn;

        private BlockChainStorage(string storageLocation, SQLiteConnection conn)
        {
            this.storageLocation = storageLocation;
            this.conn = conn;
        }

        public void Dispose()
        {
            conn.Close();
            conn.Dispose();
        }

        public static BlockChainStorage Open(string storageLocation)
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
            AttachDatabase(conn, "txhash", Path.Combine(storageLocation, TransactionHashesFilename));
            AttachDatabase(conn, "bin", Path.Combine(storageLocation, BinaryDataFilename));
            AttachDatabase(conn, "addr", Path.Combine(storageLocation, AddressesFilename));

            ApplySchemaSettings(conn, "txhash");
            ApplySchemaSettings(conn, "bin");
            ApplySchemaSettings(conn, "addr");

            //todo: handle exceptions, describe ant test them
            CheckSchema(conn);

            return new BlockChainStorage(storageLocation, conn);
        }

        private static void AttachDatabase(SQLiteConnection conn, string schema, string path)
        {
            string escapedPath = path.Replace("'", "''");
            ExecuteSql(conn, string.Format("attach '{0}' as {1}", escapedPath, schema));
        }

        private static void ApplySchemaSettings(SQLiteConnection conn, string schema)
        {
            ExecuteSql(conn, string.Format("PRAGMA {0}.journal_mode=wal", schema));
            ExecuteSql(conn, string.Format("PRAGMA {0}.synchronous=0", schema));
            ExecuteSql(conn, string.Format("PRAGMA {0}.cache_size=32000", schema));
            ExecuteSql(conn, string.Format("PRAGMA {0}.wal_autocheckpoint=8192", schema));
            ExecuteSql(conn, string.Format("PRAGMA {0}.locking_mode=EXCLUSIVE", schema));
            ExecuteSql(conn, string.Format("PRAGMA {0}.foreign_keys=ON", schema));
        }

        private static void CheckSchema(SQLiteConnection conn)
        {
            using (var tx = conn.BeginTransaction())
            {
                long hasTable;

                using (SQLiteCommand command = new SQLiteCommand("select count(*) from sqlite_master WHERE type='table' AND name='Blocks'", conn))
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
            string createSchemaSql;
            using (var stream = typeof (BlockChainStorage).Assembly.GetManifestResourceStream("BitcoinUtilities.Storage.Sql.create.sql"))
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

            //todo: use null for coinbase instead?
            SaveTransactionHashes(conn, new[] {BlockConverter.CreateTransactionHash(new byte[32])});
        }

        public void AddBlocks(List<Block> blocks)
        {
            Stopwatch sw = Stopwatch.StartNew();

            using (var tx = conn.BeginTransaction())
            {
                SaveBlocks(conn, blocks);
                SaveTransactionHashes(conn, blocks.SelectMany(b => b.Transactions).Select(t => t.Hash));
                SaveTransactionHashes(conn, blocks.SelectMany(b => b.Transactions).SelectMany(t => t.Inputs).Select(i => i.OutputHash));
                SaveAddresses(conn, blocks.SelectMany(b => b.Transactions).SelectMany(t => t.Outputs).Select(o => o.Address).Where(a => a != null));
                SaveBinaryData(conn, blocks.SelectMany(b => b.Transactions).SelectMany(t => t.Inputs).Select(i => i.SignatureScript));
                SaveBinaryData(conn, blocks.SelectMany(b => b.Transactions).SelectMany(t => t.Outputs).Select(o => o.PubkeyScript));
                
                SaveTransactions(conn, blocks.SelectMany(b => b.Transactions));
                SaveOutputs(conn, blocks.SelectMany(b => b.Transactions).SelectMany(t => t.Outputs));
                //LinkInputsToOutputs(conn, blocks.SelectMany(b => b.Transactions).SelectMany(t => t.Inputs));
                SaveInputs(conn, blocks.SelectMany(b => b.Transactions).SelectMany(t => t.Inputs));

                tx.Commit();
            }

            logger.Debug("AddBlocks took {0}ms for {1} blocks.", sw.ElapsedMilliseconds, blocks.Count);
        }

        private void SaveBlocks(SQLiteConnection conn, IEnumerable<Block> blocks)
        {
            Stopwatch sw = Stopwatch.StartNew();

            int valuesCount = 0;

            using (SQLiteCommand command = new SQLiteCommand("insert into Blocks(Hash, Height, Header)" +
                                                             "values (@Hash, @Height, @Header)", conn))
            {
                foreach (Block block in blocks)
                {
                    valuesCount++;

                    command.Parameters.Add("@Hash", DbType.Binary).Value = block.Hash;
                    command.Parameters.Add("@Height", DbType.Int32).Value = block.Height;
                    command.Parameters.Add("@Header", DbType.Binary).Value = block.Header;

                    command.ExecuteNonQuery();

                    block.Id = conn.LastInsertRowId;

                    command.Parameters.Clear();
                }
            }

            logger.Debug("SaveBlocks took {0}ms for {1} blocks.", sw.ElapsedMilliseconds, valuesCount);
        }

        private void SaveTransactions(SQLiteConnection conn, IEnumerable<Transaction> transactions)
        {
            Stopwatch sw = Stopwatch.StartNew();

            int valuesCount = 0;

            using (SQLiteCommand command = new SQLiteCommand("insert into Transactions(BlockId, NumberInBlock, HashId)" +
                                                             "values (@BlockId, @NumberInBlock, @HashId)", conn))
            {
                foreach (Transaction transaction in transactions)
                {
                    valuesCount++;

                    command.Parameters.Add("@BlockId", DbType.Int64).Value = transaction.Block.Id;
                    command.Parameters.Add("@NumberInBlock", DbType.Int32).Value = transaction.NumberInBlock;
                    command.Parameters.Add("@HashId", DbType.Int64).Value = transaction.Hash.Id;

                    command.ExecuteNonQuery();

                    transaction.Id = conn.LastInsertRowId;

                    command.Parameters.Clear();
                }
            }

            logger.Debug("SaveTransactions took {0}ms for {1} transactions.", sw.ElapsedMilliseconds, valuesCount);
        }

        private void SaveOutputs(SQLiteConnection conn, IEnumerable<TransactionOutput> outputs)
        {
            Stopwatch sw = Stopwatch.StartNew();

            int valuesCount = 0;

            using (SQLiteCommand command = new SQLiteCommand("insert into TransactionOutputs(TransactionId, NumberInTransaction, Value, PubkeyScriptId, AddressId)" +
                                                             "values (@TransactionId, @NumberInTransaction, @Value, @PubkeyScriptId, @AddressId)", conn))
            {
                foreach (TransactionOutput output in outputs)
                {
                    valuesCount++;

                    command.Parameters.Add("@TransactionId", DbType.Int64).Value = output.Transaction.Id;
                    command.Parameters.Add("@NumberInTransaction", DbType.Int32).Value = output.NumberInTransaction;
                    command.Parameters.Add("@Value", DbType.UInt64).Value = output.Value;
                    command.Parameters.Add("@PubkeyScriptId", DbType.Int64).Value = output.PubkeyScript.Id;
                    command.Parameters.Add("@AddressId", DbType.Int64).Value = output.Address == null ? null : (object) output.Address.Id;

                    command.ExecuteNonQuery();

                    output.Id = conn.LastInsertRowId;

                    command.Parameters.Clear();
                }
            }

            logger.Debug("SaveOutputs took {0}ms for {1} outputs.", sw.ElapsedMilliseconds, valuesCount);
        }

        private void SaveInputs(SQLiteConnection conn, IEnumerable<TransactionInput> inputs)
        {
            Stopwatch sw = Stopwatch.StartNew();

            int valuesCount = 0;

            using (SQLiteCommand command = new SQLiteCommand("insert into TransactionInputs(TransactionId, NumberInTransaction, SignatureScriptId, Sequence, OutputHashId, OutputIndex)" +
                                                             "values (@TransactionId, @NumberInTransaction, @SignatureScriptId, @Sequence, @OutputHashId, @OutputIndex)", conn))
            {
                foreach (TransactionInput input in inputs)
                {
                    valuesCount++;

                    command.Parameters.Add("@TransactionId", DbType.Int64).Value = input.Transaction.Id;
                    command.Parameters.Add("@NumberInTransaction", DbType.Int32).Value = input.NumberInTransaction;
                    command.Parameters.Add("@SignatureScriptId", DbType.Int64).Value = input.SignatureScript.Id;
                    command.Parameters.Add("@Sequence", DbType.UInt32).Value = input.Sequence;
                    command.Parameters.Add("@OutputHashId", DbType.Int64).Value = input.OutputHash.Id;
                    command.Parameters.Add("@OutputIndex", DbType.UInt32).Value = input.OutputIndex;

                    command.ExecuteNonQuery();

                    input.Id = conn.LastInsertRowId;

                    command.Parameters.Clear();
                }
            }

            logger.Debug("SaveInputs took {0}ms for {1} inputs.", sw.ElapsedMilliseconds, valuesCount);
        }

        //todo: reimplement
        //private void LinkInputsToOutputs(SQLiteConnection conn, IEnumerable<TransactionInput> inputs)
        //{
        //    int inputCount = 0;
        //    int outputCount = 0;
        //    Stopwatch sw = Stopwatch.StartNew();
        //
        //    //BIP-30 allows multiple transactions with the same hash, but assumes that the older ones were spent completely.
        //    //A transaction output can be spent in the same block is was created, but the order of the transactions in the block is fixed. Example: block 91859.
        //    using (SQLiteCommand command = new SQLiteCommand(@"
        //        select O.Id from TransactionOutputs O where O.TransactionId=
        //        (
        //            select T.Id from Transactions T
        //            inner join Blocks B on B.Id=T.BlockId
        //            where T.Hash=@OutputHash
        //                and (B.Height < @Height or (B.Height == @Height and T.NumberInBlock < @NumberInBlock))
        //            order by B.Height desc, T.NumberInBlock desc
        //            limit 1
        //        )
        //        and O.NumberInTransaction=@OutputIndex", conn))
        //    {
        //        foreach (TransactionInput input in inputs)
        //        {
        //            inputCount++;
        //
        //            //todo: move coinbase check to a more common place
        //            if (input.OutputIndex == 0xFFFFFFFF && input.OutputHash.All(b => b == 0))
        //            {
        //                // this is a coinbase input
        //                continue;
        //            }
        //
        //            command.Parameters.Add("@OutputHash", DbType.Binary).Value = input.OutputHash;
        //            command.Parameters.Add("@OutputIndex", DbType.UInt32).Value = input.OutputIndex;
        //            command.Parameters.Add("@Height", DbType.Int32).Value = input.Transaction.Block.Height;
        //            command.Parameters.Add("@NumberInBlock", DbType.Int32).Value = input.Transaction.NumberInBlock;
        //
        //            long? outputId = (long?) command.ExecuteScalar();
        //            if (outputId == null)
        //            {
        //                //todo: specify exception in XMLDOC
        //                throw new Exception(string.Format("A transaction has an incorrect input.\n\tBlock: {0}.\n\tTransaction: {1}.\n\tOutputHash: {2}.\n\tOutputIndex: {3}.",
        //                    input.Transaction.Block.Height,
        //                    BitConverter.ToString(input.Transaction.Hash),
        //                    BitConverter.ToString(input.OutputHash),
        //                    input.OutputIndex
        //                    ));
        //            }
        //
        //            input.Output = new TransactionOutput();
        //            input.Output.Id = outputId.Value;
        //
        //            command.Parameters.Clear();
        //
        //            outputCount++;
        //        }
        //    }
        //
        //    logger.Debug("LinkInputsToOutputs took {0}ms for {1} inputs and {2} outputs.", sw.ElapsedMilliseconds, inputCount, outputCount);
        //}

        private static void SaveBinaryData(SQLiteConnection conn, IEnumerable<BinaryData> values)
        {
            Stopwatch sw = Stopwatch.StartNew();

            int valuesCount = 0;

            using (SQLiteCommand command = new SQLiteCommand(@"insert into BinaryData(Data) values (@Data)", conn))
            {
                foreach (BinaryData data in values)
                {
                    valuesCount++;

                    command.Parameters.Add("@Data", DbType.Binary).Value = data.Data;
                    command.ExecuteNonQuery();
                    data.Id = conn.LastInsertRowId;
                    command.Parameters.Clear();
                }
            }

            logger.Debug("SaveBinaryData took {0}ms for {1} values.", sw.ElapsedMilliseconds, valuesCount);
        }

        private static void SaveAddresses(SQLiteConnection conn, IEnumerable<Address> addresses)
        {
            Stopwatch sw = Stopwatch.StartNew();

            int valuesCount = 0;

            using (SQLiteCommand findCommand = new SQLiteCommand(@"select H.Id from Addresses H where H.Value=@Value and H.SemiHash=@SemiHash", conn))
            using (SQLiteCommand addCommand = new SQLiteCommand(@"insert into Addresses(Value, SemiHash) values (@Value, @SemiHash)", conn))
            {
                foreach (Address address in addresses)
                {
                    valuesCount++;

                    findCommand.Parameters.Add("@Value", DbType.String).Value = address.Value;
                    findCommand.Parameters.Add("@SemiHash", DbType.UInt32).Value = address.SemiHash;
                    long? hashId = (long?) findCommand.ExecuteScalar();
                    findCommand.Parameters.Clear();

                    if (hashId != null)
                    {
                        address.Id = hashId.Value;
                    }
                    else
                    {
                        addCommand.Parameters.Add("@Value", DbType.String).Value = address.Value;
                        addCommand.Parameters.Add("@SemiHash", DbType.UInt32).Value = address.SemiHash;
                        addCommand.ExecuteNonQuery();
                        address.Id = conn.LastInsertRowId;
                        addCommand.Parameters.Clear();
                    }
                }
            }

            logger.Debug("SaveAddresses took {0}ms for {1} addresses.", sw.ElapsedMilliseconds, valuesCount);
        }

        public Block GetLastBlockHeader()
        {
            using (conn.BeginTransaction())
            {
                using (SQLiteCommand command = new SQLiteCommand("select B.Id, B.Hash, B.Height, B.Header from Blocks B order by B.Height desc limit 1", conn))
                {
                    using (SQLiteDataReader reader = command.ExecuteReader())
                    {
                        if (!reader.Read())
                        {
                            return null;
                        }
                        Block block = new Block();
                        int col = 0;
                        block.Id = reader.GetInt64(col++);
                        //todo: review constant usage
                        block.Hash = ReadBytes(reader, col++, 32);
                        block.Height = reader.GetInt32(col++);
                        //todo: review constant usage
                        block.Header = ReadBytes(reader, col++, 80);
                        return block;
                    }
                }
            }
        }

        private static void SaveTransactionHashes(SQLiteConnection conn, IEnumerable<TransactionHash> hashes)
        {
            Stopwatch sw = Stopwatch.StartNew();

            int valuesCount = 0;

            using (SQLiteCommand findCommand = new SQLiteCommand(@"select H.Id from TransactionHashes H where H.Hash=@Hash and H.SemiHash=@SemiHash", conn))
            using (SQLiteCommand addCommand = new SQLiteCommand(@"insert into TransactionHashes(Hash, SemiHash) values (@Hash, @SemiHash)", conn))
            {
                foreach (TransactionHash hash in hashes)
                {
                    valuesCount++;

                    findCommand.Parameters.Add("@Hash", DbType.Binary).Value = hash.Hash;
                    findCommand.Parameters.Add("@SemiHash", DbType.UInt32).Value = hash.SemiHash;
                    long? hashId = (long?) findCommand.ExecuteScalar();
                    findCommand.Parameters.Clear();

                    if (hashId != null)
                    {
                        hash.Id = hashId.Value;
                    }
                    else
                    {
                        addCommand.Parameters.Add("@Hash", DbType.Binary).Value = hash.Hash;
                        addCommand.Parameters.Add("@SemiHash", DbType.UInt32).Value = hash.SemiHash;
                        addCommand.ExecuteNonQuery();
                        hash.Id = conn.LastInsertRowId;
                        addCommand.Parameters.Clear();
                    }
                }
            }

            logger.Debug("SaveTransactionHashes took {0}ms for {1} hashes.", sw.ElapsedMilliseconds, valuesCount);
        }

        //todo: move to utils?
        private static void ExecuteSql(SQLiteConnection conn, string sql)
        {
            using (SQLiteCommand command = new SQLiteCommand(sql, conn))
            {
                command.ExecuteNonQuery();
            }
        }

        //todo: move to utils?
        private byte[] ReadBytes(SQLiteDataReader reader, int col, int expectedSize)
        {
            byte[] buffer = new byte[expectedSize];
            long bytesRead = reader.GetBytes(col, 0, buffer, 0, expectedSize);
            if (bytesRead != expectedSize)
            {
                throw new Exception(string.Format("Unexpected BLOB size in database. Expected: {0}. Read: {1}.", expectedSize, bytesRead));
            }
            byte[] extraBuffer = new byte[4];
            bytesRead = reader.GetBytes(col, expectedSize, buffer, 0, extraBuffer.Length);
            if (bytesRead > 0)
            {
                throw new Exception(string.Format("Unexpected BLOB size in database. Expected: {0}. Read at least: {1}.", expectedSize, expectedSize + bytesRead));
            }
            return buffer;
        }
    }
}