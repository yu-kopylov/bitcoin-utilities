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

        /// <summary>
        /// The maximum length of the operation log in blocks.
        /// </summary>
        public int MaximumLogLength { get; set; } = 2100;

        /// <summary>
        /// <para>The minimum allowed height of records in the operation log.</para>
        /// <para>Operations below this height will not be recorded.</para>
        /// <para>It can save time and space during downloading of old blocks, which are known to remain in the chain.</para>
        /// </summary>
        public int MinimumLogHeight { get; set; } = -1;

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

        public IReadOnlyCollection<UtxoOutput> GetUnspentOutputs(ISet<byte[]> txHashes)
        {
            var hashes = txHashes.OrderBy(h => h[0]).ToArray();
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
                    "select OutputIndex, Value, PubkeyScript from UnspentOutputs where TxHash=@TxHash",
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
                                ulong value = (ulong) reader.GetInt64(col++);
                                byte[] pubkeyScript = (byte[]) reader.GetValue(col);

                                res.Add(new UtxoOutput(new TxOutPoint(txHash, outputIndex), value, pubkeyScript));
                            }
                        }

                        command.Parameters.Clear();
                    }
                }

                tx.Commit();
            }

            return res;
        }

        public void Update(UtxoUpdate update)
        {
            Update(new UtxoUpdate[] {update});
        }

        public void Update(IReadOnlyList<UtxoUpdate> updates)
        {
            if (updates.Count == 0)
            {
                return;
            }

            CheckUpdatesAreInOrder(updates);

            UtxoAggregateUpdate aggregateUpdate = new UtxoAggregateUpdate();
            foreach (UtxoUpdate update in updates)
            {
                aggregateUpdate.Add(update.Operations);
            }

            using (var tx = MainConnection.BeginTransaction())
            {
                CheckStateBeforeUpdate(updates[0]);

                int truncateHeight = Math.Max(MinimumLogHeight, updates[updates.Count - 1].Height - MaximumLogLength);
                TruncateOperations(truncateHeight);

                DeleteUnspent(aggregateUpdate.SpentOutputs);

                InsertHeaders(updates, truncateHeight);
                InsertUnspent(aggregateUpdate.CreatedOutputs);
                InsertOperations(updates, truncateHeight);

                tx.Commit();
            }
        }

        private void CheckUpdatesAreInOrder(IReadOnlyList<UtxoUpdate> updates)
        {
            for (int i = 1; i < updates.Count; i++)
            {
                UtxoUpdate prevUpdate = updates[i - 1];
                UtxoUpdate update = updates[i];

                if (update.Height != prevUpdate.Height + 1 || !update.ParentHash.SequenceEqual(prevUpdate.HeaderHash))
                {
                    throw new InvalidOperationException("UTXO update does not follow previous update.");
                }
            }
        }

        private void CheckStateBeforeUpdate(UtxoUpdate firstUpdate)
        {
            UtxoHeader lastHeader = ReadHeader("order by Height desc");
            if (firstUpdate.Height == 0)
            {
                if (lastHeader != null)
                {
                    throw new InvalidOperationException("Cannot insert a root header into the UTXO storage, because it already has headers.");
                }
            }
            else
            {
                if (lastHeader == null || lastHeader.Height != firstUpdate.Height - 1 || !lastHeader.Hash.SequenceEqual(firstUpdate.ParentHash))
                {
                    throw new InvalidOperationException("The last header in the UTXO storage does not match the parent header of the update.");
                }
            }
        }

        private void InsertHeaders(IReadOnlyList<UtxoUpdate> updates, int truncateHeight)
        {
            using (SQLiteCommand command = new SQLiteCommand(
                "insert into Headers(Hash, Height, IsReversible)" +
                "values (@Hash, @Height, @IsReversible)",
                MainConnection
            ))
            {
                foreach (var update in updates)
                {
                    command.Parameters.Add("@Hash", DbType.Binary).Value = update.HeaderHash;
                    command.Parameters.Add("@Height", DbType.Int32).Value = update.Height;
                    command.Parameters.Add("@IsReversible", DbType.Boolean).Value = update.Height > truncateHeight;

                    command.ExecuteNonQuery();
                    command.Parameters.Clear();
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
                    bool isReversible = reader.GetBoolean(col);

                    return new UtxoHeader(hash, height, isReversible);
                }
            }
        }

        private void DeleteUnspent(IEnumerable<TxOutPoint> outputs)
        {
            using (SQLiteCommand command = new SQLiteCommand(
                "delete from UnspentOutputs where TxHash=@TxHash and OutputIndex=@OutputIndex",
                MainConnection
            ))
            {
                foreach (TxOutPoint output in outputs.OrderBy(p => p.Hash[0]))
                {
                    command.Parameters.Add("@TxHash", DbType.Binary).Value = output.Hash;
                    command.Parameters.Add("@OutputIndex", DbType.Int32).Value = output.Index;
                    int affectedRows = command.ExecuteNonQuery();
                    if (affectedRows == 0)
                    {
                        throw new InvalidOperationException(
                            $"Attempt to spend a non-existing output '{HexUtils.GetString(output.Hash)}:{output.Index}'."
                        );
                    }

                    command.Parameters.Clear();
                }
            }
        }

        private void InsertUnspent(IEnumerable<UtxoOutput> unspentOutputs)
        {
            using (SQLiteCommand command = new SQLiteCommand(
                "insert into UnspentOutputs(TxHash, OutputIndex, Value, PubkeyScript)" +
                "values (@TxHash, @OutputIndex, @Value, @PubkeyScript)",
                MainConnection
            ))
            {
                foreach (UtxoOutput output in unspentOutputs.OrderBy(p => p.OutPoint.Hash[0]))
                {
                    command.Parameters.Add("@TxHash", DbType.Binary).Value = output.OutPoint.Hash;
                    command.Parameters.Add("@OutputIndex", DbType.Int32).Value = output.OutPoint.Index;
                    command.Parameters.Add("@Value", DbType.UInt64).Value = output.Value;
                    command.Parameters.Add("@PubkeyScript", DbType.Binary).Value = output.PubkeyScript;

                    command.ExecuteNonQuery();
                    command.Parameters.Clear();
                }
            }
        }

        private void InsertOperations(IReadOnlyList<UtxoUpdate> updates, int truncateHeight)
        {
            var updatesToLog = updates.Where(u => u.Height > truncateHeight).ToList();

            if (!updatesToLog.Any())
            {
                return;
            }

            using (SQLiteCommand command = new SQLiteCommand(
                "insert into OutputOperations(Height, OperationNumber, Spent, TxHash, OutputIndex, Value, PubkeyScript)" +
                "values (@Height, @OperationNumber, @Spent, @TxHash, @OutputIndex, @Value, @PubkeyScript)",
                MainConnection
            ))
            {
                foreach (var update in updatesToLog)
                {
                    int operationNumber = 0;
                    foreach (UtxoOperation operation in update.Operations)
                    {
                        command.Parameters.Add("@Height", DbType.Int32).Value = update.Height;
                        command.Parameters.Add("@OperationNumber", DbType.Int32).Value = operationNumber++;
                        command.Parameters.Add("@Spent", DbType.Boolean).Value = operation.Spent;
                        command.Parameters.Add("@TxHash", DbType.Binary).Value = operation.Output.OutPoint.Hash;
                        command.Parameters.Add("@OutputIndex", DbType.Int32).Value = operation.Output.OutPoint.Index;
                        command.Parameters.Add("@Value", DbType.UInt64).Value = operation.Output.Value;
                        command.Parameters.Add("@PubkeyScript", DbType.Binary).Value = operation.Output.PubkeyScript;

                        command.ExecuteNonQuery();
                        command.Parameters.Clear();
                    }
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

                var lastHeader = ReadHeader("order by Height desc");
                while (lastHeader.Height > targetHeader.Height)
                {
                    Revert(lastHeader);
                    lastHeader = ReadHeader("order by Height desc");
                }

                tx.Commit();
            }
        }

        /// <summary>
        /// Removes undo information for the header with the given height and all headers before it.
        /// After this information is removed, it will be impossible to revert those header.
        /// </summary>
        /// <param name="height">The height of the header that will no longer be reversible.</param>
        private void TruncateOperations(int height)
        {
            MainConnection.ExecuteNonQuery("update Headers set IsReversible=0 where Height <= @Height and IsReversible != 0", p => p.AddWithValue("@Height", height));
            MainConnection.ExecuteNonQuery("delete from OutputOperations where Height <= @Height", p => p.AddWithValue("@Height", height));
        }

        private void Revert(UtxoHeader header)
        {
            if (!header.IsReversible)
            {
                throw new InvalidOperationException(
                    $"Cannot revert operations for header '{HexUtils.GetString(header.Hash)}', because it is not reversible."
                );
            }

            int removedHeaders = MainConnection.ExecuteNonQuery(
                "delete from Headers where Height=@Height and Hash=@Hash and IsReversible=1",
                p => p.AddWithValue("@Height", header.Height),
                p => p.AddWithValue("@Hash", header.Hash)
            );

            if (removedHeaders != 1)
            {
                throw new InvalidOperationException(
                    $"Storage changed. Cannot delete reversible header with hash '{HexUtils.GetString(header.Hash)}' and height {header.Height}."
                );
            }

            List<UtxoOperation> operations = new List<UtxoOperation>();

            using (SQLiteCommand command = new SQLiteCommand(
                "select Spent, TxHash, OutputIndex, Value, PubkeyScript from OutputOperations" +
                " where Height=@Height" +
                " order by OperationNumber asc",
                MainConnection
            ))
            {
                command.Parameters.AddWithValue("@Height", header.Height);
                using (var reader = command.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        int col = 0;
                        bool spent = reader.GetBoolean(col++);
                        byte[] txHash = (byte[]) reader.GetValue(col++);
                        int outputIndex = reader.GetInt32(col++);
                        ulong value = (ulong) reader.GetInt64(col++);
                        byte[] pubkeyScript = (byte[]) reader.GetValue(col);

                        operations.Add(new UtxoOperation(spent, new UtxoOutput(new TxOutPoint(txHash, outputIndex), value, pubkeyScript)));
                    }
                }
            }

            MainConnection.ExecuteNonQuery("delete from OutputOperations where Height=@Height", p => p.AddWithValue("@Height", header.Height));

            operations.Reverse();

            UtxoAggregateUpdate aggregateUpdate = new UtxoAggregateUpdate();
            aggregateUpdate.AddReversals(operations);

            DeleteUnspent(aggregateUpdate.SpentOutputs);
            InsertUnspent(aggregateUpdate.CreatedOutputs);
        }
    }
}