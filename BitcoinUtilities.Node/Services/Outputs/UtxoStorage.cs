using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SQLite;
using System.Linq;
using BitcoinUtilities.P2P.Primitives;
using BitcoinUtilities.Threading;
using NLog;

namespace BitcoinUtilities.Node.Services.Outputs
{
    /// <summary>
    /// Stores unspent outputs in an SQLite database.
    /// </summary>
    public class UtxoStorage : IDisposable
    {
        private static readonly ILogger logger = LogManager.GetCurrentClassLogger();

        private readonly SQLiteConnection[] connections;

        private UtxoStorage(SQLiteConnection[] connections)
        {
            this.connections = connections;
        }

        public void Dispose()
        {
            foreach (var connection in connections)
            {
                connection.Close();
                connection.Dispose();
            }
        }

        private SQLiteConnection MainConnection
        {
            get { return connections[0]; }
        }

        public static UtxoStorage Open(string filename)
        {
            SQLiteConnectionStringBuilder connStrBuilder = new SQLiteConnectionStringBuilder();
            connStrBuilder.DataSource = filename;
            connStrBuilder.JournalMode = SQLiteJournalModeEnum.Wal;
            connStrBuilder.SyncMode = SynchronizationModes.Off;
            connStrBuilder.DefaultIsolationLevel = IsolationLevel.ReadCommitted;

            const int connectionCount = 4;
            SQLiteConnection[] connections = new SQLiteConnection[connectionCount];

            try
            {
                for (int i = 0; i < connectionCount; i++)
                {
                    var conn = new SQLiteConnection(connStrBuilder.ConnectionString);
                    connections[i] = conn;
                    conn.Open();
                }

                try
                {
                    SqliteUtils.CheckSchema(connections[0], "Headers", typeof(UtxoStorage), "utxo-create.sql");
                }
                catch (Exception ex)
                {
                    logger.Error(ex, $"Failed to verify schema in '{filename}'.");
                    throw;
                }
            }
            catch (Exception)
            {
                foreach (SQLiteConnection connection in connections)
                {
                    if (connection != null)
                    {
                        connection.Close();
                        connection.Dispose();
                    }
                }

                throw;
            }

            return new UtxoStorage(connections);
        }

        public UtxoHeader GetLastHeader()
        {
            UtxoHeader bestHeader;

            using (var tx = MainConnection.BeginTransaction())
            {
                bestHeader = ReadHeader("order by Height desc");
                tx.Commit();
            }

            return bestHeader;
        }

        public IReadOnlyCollection<UtxoOutput> GetUnspentOutputs(IEnumerable<byte[]> txHashes)
        {
            var hashes = txHashes.Select(CopyHash).OrderBy(h => h[0]).ToArray();
            var enumerator = new ConcurrentEnumerator<byte[]>(hashes);

            List<List<UtxoOutput>> partialResults = enumerator.ProcessWith(connections, GetUnspentOutputs);

            List<UtxoOutput> res = new List<UtxoOutput>();
            foreach (List<UtxoOutput> partialResult in partialResults)
            {
                res.AddRange(partialResult);
            }

            return res;
        }

        private static List<UtxoOutput> GetUnspentOutputs(SQLiteConnection conn, IConcurrentEnumerator<byte[]> txHashes)
        {
            List<UtxoOutput> res = new List<UtxoOutput>();

            using (var tx = conn.BeginTransaction())
            {
                using (SQLiteCommand command = new SQLiteCommand(
                    "select OutputIndex, Height, Value, Script from UnspentOutputs where TxHash=@TxHash",
                    conn
                ))
                {
                    while (txHashes.GetNext(out var txHash))
                    {
                        command.Parameters.Add("@TxHash", DbType.Binary).Value = txHash;

                        using (var reader = command.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                int col = 0;

                                int outputIndex = reader.GetInt32(col++);
                                int height = reader.GetInt32(col++);
                                ulong value = (ulong) reader.GetInt64(col++);
                                byte[] script = (byte[]) reader.GetValue(col++);

                                res.Add(new UtxoOutput(new TxOutPoint(txHash, outputIndex), height, value, script, -1));
                            }
                        }

                        command.Parameters.Clear();
                    }
                }

                tx.Commit();
            }

            return res;
        }

        // todo: move to utils?
        private byte[] CopyHash(byte[] hash)
        {
            // hash is copied to preserve output in an unlikely case of input array mutation
            byte[] res = new byte[hash.Length];
            Array.Copy(hash, 0, res, 0, hash.Length);
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

            using (var tx = MainConnection.BeginTransaction())
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
                MainConnection
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
            using (SQLiteCommand command = new SQLiteCommand($"select Hash, Height, IsReversible from Headers {filterAndOrder} LIMIT 1", MainConnection))
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
                MainConnection
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
                MainConnection
            ))
            {
                foreach (UtxoOutput output in unspentOutputs.OrderBy(p => p.Height))
                {
                    command.Parameters.Add("@TxHash", DbType.Binary).Value = output.OutputPoint.Hash;
                    command.Parameters.Add("@OutputIndex", DbType.Int32).Value = output.OutputPoint.Index;
                    command.Parameters.Add("@Height", DbType.Int32).Value = output.Height;
                    command.Parameters.Add("@Value", DbType.UInt64).Value = output.Value;
                    command.Parameters.Add("@Script", DbType.Binary).Value = output.PubkeyScript;
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
                MainConnection
            ))
            {
                foreach (UtxoOutput output in unspentOutputs.OrderBy(p => p.OutputPoint.Hash[0]))
                {
                    command.Parameters.Add("@TxHash", DbType.Binary).Value = output.OutputPoint.Hash;
                    command.Parameters.Add("@OutputIndex", DbType.Int32).Value = output.OutputPoint.Index;
                    command.Parameters.Add("@Height", DbType.Int32).Value = output.Height;
                    command.Parameters.Add("@Value", DbType.UInt64).Value = output.Value;
                    // todo: rename Script column to PubkeyScript
                    command.Parameters.Add("@Script", DbType.Binary).Value = output.PubkeyScript;

                    command.ExecuteNonQuery();
                    command.Parameters.Clear();
                }
            }
        }

        public void RevertTo(byte[] headerHash)
        {
            using (var tx = MainConnection.BeginTransaction())
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

                MainConnection.ExecuteNonQuery(
                    "delete from Headers where Height > @Height",
                    p => p.AddWithValue("@Height", targetHeader.Height)
                );
                MainConnection.ExecuteNonQuery(
                    "delete from UnspentOutputs where Height > @Height",
                    p => p.AddWithValue("@Height", targetHeader.Height)
                );
                MainConnection.ExecuteNonQuery(
                    "insert into UnspentOutputs (TxHash, OutputIndex, Height, Value, Script)" +
                    "select TxHash, OutputIndex, Height, Value, Script from SpentOutputs where SpentHeight > @Height and Height <= @Height",
                    p => p.AddWithValue("@Height", targetHeader.Height)
                );
                MainConnection.ExecuteNonQuery(
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
            using (var tx = MainConnection.BeginTransaction())
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
                    // nothing to do, because rollback information was already removed
                    return;
                }

                MainConnection.ExecuteNonQuery("update Headers set IsReversible=0 where Height <= @Height", p => p.AddWithValue("@Height", header.Height));
                MainConnection.ExecuteNonQuery("delete from SpentOutputs where SpentHeight <= @Height", p => p.AddWithValue("@Height", header.Height));

                tx.Commit();
            }
        }
    }
}