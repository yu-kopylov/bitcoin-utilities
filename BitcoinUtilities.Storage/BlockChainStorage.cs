using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SQLite;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using BitcoinUtilities.Storage.Models;
using NLog;

namespace BitcoinUtilities.Storage
{
    public class BlockChainStorage
    {
        private static readonly Logger logger = LogManager.GetCurrentClassLogger();

        private readonly string connectionString;

        private BlockChainStorage(string connectionString)
        {
            this.connectionString = connectionString;
        }

        public static BlockChainStorage Open(string dbFilename)
        {
            string connectionString = string.Format("Data Source=\"{0}\";", dbFilename);

            using (SQLiteConnection conn = new SQLiteConnection(connectionString))
            {
                //todo: handle exceptions, describe ant test them
                conn.Open();
                CheckSchema(conn);
            }

            return new BlockChainStorage(connectionString);
        }

        private static void CheckSchema(SQLiteConnection conn)
        {
            using (var tx = conn.BeginTransaction())
            {
                SQLiteCommand command = new SQLiteCommand("select count(*) from sqlite_master WHERE type='table' AND name='Blocks'", conn);
                long hasTable = (long) command.ExecuteScalar();

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
            SQLiteCommand command = new SQLiteCommand(createSchemaSql, conn);
            command.ExecuteNonQuery();
        }

        public void AddBlocks(List<Block> blocks)
        {
            Stopwatch sw = Stopwatch.StartNew();

            using (SQLiteConnection conn = new SQLiteConnection(connectionString))
            {
                conn.Open();
                using (var tx = conn.BeginTransaction())
                {
                    SaveBlocks(conn, blocks);
                    SaveTransactions(conn, blocks.SelectMany(b => b.Transactions));
                    SaveOutputs(conn, blocks.SelectMany(b => b.Transactions).SelectMany(t => t.Outputs));
                    LinkInputsToOutputs(conn, blocks.SelectMany(b => b.Transactions).SelectMany(t => t.Inputs));
                    SaveInputs(conn, blocks.SelectMany(b => b.Transactions).SelectMany(t => t.Inputs));
                    tx.Commit();
                }
            }

            logger.Debug("AddBlocks took {0}ms.", sw.ElapsedMilliseconds);
        }

        private void SaveBlocks(SQLiteConnection conn, IEnumerable<Block> blocks)
        {
            using (SQLiteCommand command = new SQLiteCommand("insert into Blocks(Hash, Height, Header)" +
                                                             "values (@Hash, @Height, @Header)", conn))
            {
                foreach (Block block in blocks)
                {
                    command.Parameters.Add("@Hash", DbType.Binary).Value = block.Hash;
                    command.Parameters.Add("@Height", DbType.Int32).Value = block.Height;
                    command.Parameters.Add("@Header", DbType.Binary).Value = block.Header;

                    command.ExecuteNonQuery();

                    block.Id = conn.LastInsertRowId;

                    command.Parameters.Clear();
                }
            }
        }

        private void SaveTransactions(SQLiteConnection conn, IEnumerable<Transaction> transactions)
        {
            using (SQLiteCommand command = new SQLiteCommand("insert into Transactions(Hash, BlockId, NumberInBlock)" +
                                                             "values (@Hash, @BlockId, @NumberInBlock)", conn))
            {
                foreach (Transaction transaction in transactions)
                {
                    command.Parameters.Add("@Hash", DbType.Binary).Value = transaction.Hash;
                    command.Parameters.Add("@BlockId", DbType.Int64).Value = transaction.Block.Id;
                    command.Parameters.Add("@NumberInBlock", DbType.Int32).Value = transaction.NumberInBlock;

                    command.ExecuteNonQuery();

                    transaction.Id = conn.LastInsertRowId;

                    command.Parameters.Clear();
                }
            }
        }

        private void SaveOutputs(SQLiteConnection conn, IEnumerable<TransactionOutput> outputs)
        {
            using (SQLiteCommand command = new SQLiteCommand("insert into TransactionOutputs(TransactionId, NumberInTransaction, Value, PubkeyScriptType, PubkeyScript, PublicKey)" +
                                                             "values (@TransactionId, @NumberInTransaction, @Value, @PubkeyScriptType, @PubkeyScript, @PublicKey)", conn))
            {
                foreach (TransactionOutput output in outputs)
                {
                    command.Parameters.Add("@TransactionId", DbType.Int64).Value = output.Transaction.Id;
                    command.Parameters.Add("@NumberInTransaction", DbType.Int32).Value = output.NumberInTransaction;
                    command.Parameters.Add("@Value", DbType.UInt64).Value = output.Value;
                    command.Parameters.Add("@PubkeyScriptType", DbType.Int32).Value = (int) output.PubkeyScriptType;
                    command.Parameters.Add("@PubkeyScript", DbType.Binary).Value = output.PubkeyScript;
                    command.Parameters.Add("@PublicKey", DbType.Binary).Value = output.PublicKey;

                    command.ExecuteNonQuery();

                    output.Id = conn.LastInsertRowId;

                    command.Parameters.Clear();
                }
            }
        }

        private void SaveInputs(SQLiteConnection conn, IEnumerable<TransactionInput> inputs)
        {
            using (SQLiteCommand command = new SQLiteCommand("insert into TransactionInputs(TransactionId, NumberInTransaction, SignatureScript, Sequence, OutputHash, OutputIndex, OutputId)" +
                                                             "values (@TransactionId, @NumberInTransaction, @SignatureScript, @Sequence, @OutputHash, @OutputIndex, @OutputId)", conn))
            {
                foreach (TransactionInput input in inputs)
                {
                    command.Parameters.Add("@TransactionId", DbType.Int64).Value = input.Transaction.Id;
                    command.Parameters.Add("@NumberInTransaction", DbType.Int32).Value = input.NumberInTransaction;
                    command.Parameters.Add("@SignatureScript", DbType.Binary).Value = input.SignatureScript;
                    command.Parameters.Add("@Sequence", DbType.UInt32).Value = input.Sequence;
                    command.Parameters.Add("@OutputHash", DbType.Binary).Value = input.OutputHash;
                    command.Parameters.Add("@OutputIndex", DbType.UInt32).Value = input.OutputIndex;
                    command.Parameters.Add("@OutputId", DbType.UInt64).Value = input.Output == null ? null : (long?) input.Output.Id;

                    command.ExecuteNonQuery();

                    input.Id = conn.LastInsertRowId;

                    command.Parameters.Clear();
                }
            }
        }

        private void LinkInputsToOutputs(SQLiteConnection conn, IEnumerable<TransactionInput> inputs)
        {
            int inputCount = 0;
            int outputCount = 0;
            Stopwatch sw = Stopwatch.StartNew();

            using (SQLiteCommand command = new SQLiteCommand("select O.Id " +
                                                             "from Transactions T " +
                                                             "inner join TransactionOutputs O on O.TransactionId=T.Id " +
                                                             "where T.Hash=@OutputHash and O.NumberInTransaction=@OutputIndex", conn))
            {
                foreach (TransactionInput input in inputs)
                {
                    inputCount++;

                    //todo: move coinbase check to a more common place
                    if (input.OutputIndex == 0xFFFFFFFF && input.OutputHash.All(b => b == 0))
                    {
                        // this is a coinbase input
                        continue;
                    }

                    command.Parameters.Add("@OutputHash", DbType.Binary).Value = input.OutputHash;
                    command.Parameters.Add("@OutputIndex", DbType.UInt32).Value = input.OutputIndex;

                    long? outputId = (long?) command.ExecuteScalar();
                    if (outputId == null)
                    {
                        //todo: specify exception in XMLDOC
                        throw new Exception(string.Format("A transaction has an incorrect input.\n\tBlock: {0}.\n\tTransaction: {1}.\n\tOutputHash: {2}.\n\tOutputIndex: {3}.",
                            input.Transaction.Block.Height,
                            BitConverter.ToString(input.Transaction.Hash),
                            BitConverter.ToString(input.OutputHash),
                            input.OutputIndex
                            ));
                    }

                    input.Output = new TransactionOutput();
                    input.Output.Id = outputId.Value;

                    command.Parameters.Clear();

                    outputCount++;
                }
            }

            logger.Debug("LinkInputsToOutputs took {0}ms for {1} inputs and {2} outputs.", sw.ElapsedMilliseconds, inputCount, outputCount);
        }

        public Block GetLastBlockHeader()
        {
            using (SQLiteConnection conn = new SQLiteConnection(connectionString))
            {
                conn.Open();
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