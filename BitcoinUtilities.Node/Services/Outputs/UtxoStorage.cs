using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SQLite;
using System.Linq;
using BitcoinUtilities.Collections;
using BitcoinUtilities.P2P.Primitives;
using NLog;

namespace BitcoinUtilities.Node.Services.Outputs
{
    /// <summary>
    /// Stores unspent outputs in an SQLite database.
    /// </summary>
    public class UtxoStorage : IDisposable
    {
        private static readonly ILogger logger = LogManager.GetCurrentClassLogger();

        private readonly SQLiteConnection conn;

        private UtxoStorage(SQLiteConnection conn)
        {
            this.conn = conn;
        }

        public void Dispose()
        {
            conn.Close();
            conn.Dispose();
        }

        public static UtxoStorage Open(string filename)
        {
            SQLiteConnectionStringBuilder connStrBuilder = new SQLiteConnectionStringBuilder();
            connStrBuilder.DataSource = filename;
            connStrBuilder.JournalMode = SQLiteJournalModeEnum.Wal;
            connStrBuilder.SyncMode = SynchronizationModes.Off;

            var conn = new SQLiteConnection(connStrBuilder.ConnectionString);
            conn.Open();

            try
            {
                SqliteUtils.CheckSchema(conn, "Headers", typeof(UtxoStorage), "utxo-create.sql");
            }
            catch (Exception ex)
            {
                logger.Error(ex, $"Failed to verify schema in '{filename}'.");
                conn.Close();
                conn.Dispose();
                throw;
            }

            return new UtxoStorage(conn);
        }

        public UtxoHeader GetLastHeader()
        {
            UtxoHeader bestHeader;

            using (var tx = conn.BeginTransaction())
            {
                bestHeader = ReadHeader("order by Height desc");
                tx.Commit();
            }

            return bestHeader;
        }

        public IReadOnlyCollection<UtxoOutput> GetUnspentOutputs(IEnumerable<TxOutPoint> outputPoints)
        {
            List<UtxoOutput> res = new List<UtxoOutput>();

            using (var tx = conn.BeginTransaction())
            {
                foreach (List<byte[]> chunk in outputPoints.Select(o => o.Hash).Distinct(ByteArrayComparer.Instance).OrderBy(h => h[0]).Split(900))
                {
                    //todo: compare 1-by-1 select vs IN
                    using (SQLiteCommand command = new SQLiteCommand(
                        "select TxHash, OutputIndex, Height, Value, Script from UnspentOutputs" +
                        $" where TxHash in ({SqliteUtils.GetInParameters("H", chunk.Count)})",
                        conn
                    ))
                    {
                        SqliteUtils.SetInParameters(command.Parameters, "H", DbType.Binary, chunk);

                        using (var reader = command.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                int col = 0;

                                byte[] txHash = (byte[]) reader[col++];
                                int outputIndex = reader.GetInt32(col++);
                                int height = reader.GetInt32(col++);
                                ulong value = (ulong) reader.GetInt64(col++);
                                byte[] script = (byte[]) reader[col++];

                                res.Add(new UtxoOutput(new TxOutPoint(txHash, outputIndex), height, value, script, -1));
                            }
                        }
                    }
                }

                //todo: filter output or change signature

                tx.Commit();
            }

            return res;
        }

        public void Update(UtxoUpdate update)
        {
            Update(new UtxoUpdate[] {update});
        }

        public void Update(IReadOnlyCollection<UtxoUpdate> updates)
        {
            UtxoAggregateUpdate aggregateUpdate = new UtxoAggregateUpdate();
            foreach (UtxoUpdate update in updates)
            {
                aggregateUpdate.Add(update);
            }

            using (var tx = conn.BeginTransaction())
            {
                CheckStateBeforeUpdate(aggregateUpdate);

                InsertHeaders(aggregateUpdate.FirstHeaderHeight, aggregateUpdate.HeaderHashes);
                DeleteUnspent(aggregateUpdate.ExistingSpentOutputs);
                InsertUnspent(aggregateUpdate.UnspentOutputs.Values);
                InsertSpent(aggregateUpdate.AllSpentOutputs);

                tx.Commit();
            }
        }

        private void CheckStateBeforeUpdate(UtxoAggregateUpdate aggregateUpdate)
        {
            UtxoHeader lastHeader = ReadHeader("order by Height desc");
            if (aggregateUpdate.FirstHeaderHeight == 0)
            {
                if (lastHeader != null)
                {
                    throw new InvalidOperationException("Cannot insert a root header into the UTXO storage, because it already has headers.");
                }
            }
            else
            {
                if (lastHeader == null
                    || lastHeader.Height != aggregateUpdate.FirstHeaderHeight - 1
                    || !ByteArrayComparer.Instance.Equals(lastHeader.Hash, aggregateUpdate.FirstHeaderParent))
                {
                    throw new InvalidOperationException("The last header in the UTXO storage does not match the parent header of the update.");
                }
            }
        }

        private void InsertHeaders(int firstHeaderHeight, List<byte[]> headerHashes)
        {
            int height = firstHeaderHeight;

            using (SQLiteCommand command = new SQLiteCommand(
                "insert into Headers(Hash, Height, IsReversible)" +
                "values (@Hash, @Height, 1)",
                conn
            ))
            {
                foreach (byte[] headerHash in headerHashes)
                {
                    command.Parameters.Add("@Hash", DbType.Binary).Value = headerHash;
                    command.Parameters.Add("@Height", DbType.Int32).Value = height;

                    command.ExecuteNonQuery();
                    command.Parameters.Clear();

                    height++;
                }
            }
        }

        private UtxoHeader ReadHeader(string filterAndOrder, params Action<SQLiteParameterCollection>[] parameterSetters)
        {
            using (SQLiteCommand command = new SQLiteCommand($"select Hash, Height, IsReversible from Headers {filterAndOrder} LIMIT 1", conn))
            {
                foreach (var parameterSetter in parameterSetters)
                {
                    parameterSetter(command.Parameters);
                }

                using (var reader = command.ExecuteReader())
                {
                    if (!reader.Read())
                    {
                        return null;
                    }

                    int col = 0;

                    byte[] hash = (byte[]) reader[col++];
                    int height = reader.GetInt32(col++);
                    bool isReversible = reader.GetBoolean(col++);

                    return new UtxoHeader(hash, height, isReversible);
                }
            }
        }

        private void DeleteUnspent(List<UtxoOutput> outputs)
        {
            using (SQLiteCommand command = new SQLiteCommand(
                "delete from UnspentOutputs where TxHash=@TxHash and OutputIndex=@OutputIndex",
                conn
            ))
            {
                foreach (UtxoOutput output in outputs.OrderBy(p => p.OutputPoint.Hash[0]))
                {
                    command.Parameters.Add("@TxHash", DbType.Binary).Value = output.OutputPoint.Hash;
                    command.Parameters.Add("@OutputIndex", DbType.Int32).Value = output.OutputPoint.Index;
                    int affectedRows = command.ExecuteNonQuery();
                    if (affectedRows == 0)
                    {
                        throw new InvalidOperationException(
                            $"Attempt to spend a non-existing output '{HexUtils.GetString(output.OutputPoint.Hash)}:{output.OutputPoint.Index}'."
                        );
                    }

                    command.Parameters.Clear();
                }
            }
        }

        private void InsertSpent(IEnumerable<UtxoOutput> unspentOutputs)
        {
            using (SQLiteCommand command = new SQLiteCommand(
                "insert into SpentOutputs(TxHash, OutputIndex, Height, Value, Script, SpentHeight)" +
                "values (@TxHash, @OutputIndex, @Height, @Value, @Script, @SpentHeight)",
                conn
            ))
            {
                foreach (UtxoOutput output in unspentOutputs.OrderBy(p => p.Height))
                {
                    command.Parameters.Add("@TxHash", DbType.Binary).Value = output.OutputPoint.Hash;
                    command.Parameters.Add("@OutputIndex", DbType.Int32).Value = output.OutputPoint.Index;
                    command.Parameters.Add("@Height", DbType.Int32).Value = output.Height;
                    command.Parameters.Add("@Value", DbType.UInt64).Value = output.Value;
                    command.Parameters.Add("@Script", DbType.Binary).Value = output.Script;
                    command.Parameters.Add("@SpentHeight", DbType.Int32).Value = output.SpentHeight;

                    command.ExecuteNonQuery();
                    command.Parameters.Clear();
                }
            }
        }

        private void InsertUnspent(IEnumerable<UtxoOutput> unspentOutputs)
        {
            using (SQLiteCommand command = new SQLiteCommand(
                "insert into UnspentOutputs(TxHash, OutputIndex, Height, Value, Script)" +
                "values (@TxHash, @OutputIndex, @Height, @Value, @Script)",
                conn
            ))
            {
                foreach (UtxoOutput output in unspentOutputs.OrderBy(p => p.OutputPoint.Hash[0]))
                {
                    command.Parameters.Add("@TxHash", DbType.Binary).Value = output.OutputPoint.Hash;
                    command.Parameters.Add("@OutputIndex", DbType.Int32).Value = output.OutputPoint.Index;
                    command.Parameters.Add("@Height", DbType.Int32).Value = output.Height;
                    command.Parameters.Add("@Value", DbType.UInt64).Value = output.Value;
                    command.Parameters.Add("@Script", DbType.Binary).Value = output.Script;

                    command.ExecuteNonQuery();
                    command.Parameters.Clear();
                }
            }
        }

        public void RevertTo(byte[] headerHash)
        {
            using (var tx = conn.BeginTransaction())
            {
                var targetHeader = ReadHeader("where Hash=@Hash", p => p.AddWithValue("@Hash", headerHash));
                if (targetHeader == null)
                {
                    throw new InvalidOperationException($"The UTXO storage does not have a header with hash '{HexUtils.GetString(headerHash)}'.");
                }

                var nextHeader = ReadHeader("where Height=@Height", p => p.AddWithValue("@Height", targetHeader.Height + 1));
                if (nextHeader == null)
                {
                    // nothing to do, the target header is already the last header
                    return;
                }

                if (!nextHeader.IsReversible)
                {
                    throw new InvalidOperationException(
                        $"Cannot revert to the header with hash '{HexUtils.GetString(headerHash)}', because the following header is not reversible."
                    );
                }

                conn.ExecuteNonQuery(
                    "delete from Headers where Height > @Height",
                    p => p.AddWithValue("@Height", targetHeader.Height)
                );
                conn.ExecuteNonQuery(
                    "delete from UnspentOutputs where Height > @Height",
                    p => p.AddWithValue("@Height", targetHeader.Height)
                );
                conn.ExecuteNonQuery(
                    "insert into UnspentOutputs (TxHash, OutputIndex, Height, Value, Script)" +
                    "select TxHash, OutputIndex, Height, Value, Script from SpentOutputs where SpentHeight > @Height and Height <= @Height",
                    p => p.AddWithValue("@Height", targetHeader.Height)
                );
                conn.ExecuteNonQuery(
                    "delete from SpentOutputs where SpentHeight > @Height",
                    p => p.AddWithValue("@Height", targetHeader.Height)
                );

                tx.Commit();
            }
        }

        /// <summary>
        /// Removes undo information for the header with the given hash and all headers before it.
        /// After this information is removed, it will be impossible to revert those header.
        /// </summary>
        /// <param name="headerHash">The hash of the header that will no longer be reversible.</param>
        public void Truncate(byte[] headerHash)
        {
            using (var tx = conn.BeginTransaction())
            {
                UtxoHeader header = ReadHeader("where Hash=@Hash", p => p.AddWithValue("@Hash", headerHash));

                if (header == null)
                {
                    throw new InvalidOperationException(
                        $"The UTXO storage does not have a header with hash '{HexUtils.GetString(headerHash)}'."
                    );
                }

                if (!header.IsReversible)
                {
                    // nothig to do, because rollback information was already removed
                    return;
                }

                conn.ExecuteNonQuery("update Headers set IsReversible=0 where Height <= @Height", p => p.AddWithValue("@Height", header.Height));
                conn.ExecuteNonQuery("delete from SpentOutputs where SpentHeight <= @Height", p => p.AddWithValue("@Height", header.Height));

                tx.Commit();
            }
        }
    }
}