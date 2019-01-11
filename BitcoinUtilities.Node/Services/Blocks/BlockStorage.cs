using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SQLite;
using System.IO;

namespace BitcoinUtilities.Node.Services.Blocks
{
    public class BlockStorage : IDisposable
    {
        private const string BlocksFilename = "blocks2.db";

        private readonly object monitor = new object();

        private readonly SQLiteConnection conn;

        public BlockStorage(SQLiteConnection conn)
        {
            this.conn = conn;
        }

        public void Dispose()
        {
            conn.Close();
            conn.Dispose();
        }

        internal long MaxUnrequestedBlocksVolume { get; set; } = 1 * 1024L * 1024 * 1024;

        public static BlockStorage Open(string storageLocation)
        {
            if (!Directory.Exists(storageLocation))
            {
                Directory.CreateDirectory(storageLocation);
            }

            SQLiteConnectionStringBuilder connStrBuilder = new SQLiteConnectionStringBuilder();
            connStrBuilder.DataSource = Path.Combine(storageLocation, BlocksFilename);
            connStrBuilder.JournalMode = SQLiteJournalModeEnum.Wal;
            string connStr = connStrBuilder.ConnectionString;

            SQLiteConnection conn = new SQLiteConnection(connStr);
            conn.Open();

            //todo: handle exceptions, describe ant test them
            SqliteUtils.CheckSchema(conn, "BlockRequests", typeof(BlockStorage), "create.sql");

            return new BlockStorage(conn);
        }

        public void AddBlock(byte[] hash, byte[] content)
        {
            lock (monitor)
            {
                //todo: review
                using (var tx = conn.BeginTransaction())
                {
                    bool requested;

                    using (SQLiteCommand command = new SQLiteCommand(
                        "select ifnull((select 1 from Blocks where Hash=@Hash limit 1), 0)",
                        conn
                    ))
                    {
                        command.Parameters.Add("@Hash", DbType.Binary).Value = hash;
                        bool exists = (long) command.ExecuteScalar() == 1;
                        if (exists)
                        {
                            return;
                        }
                    }

                    using (SQLiteCommand command = new SQLiteCommand(
                        "select ifnull((select 1 from BlockRequests where Hash=@Hash limit 1), 0)",
                        conn
                    ))
                    {
                        command.Parameters.Add("@Hash", DbType.Binary).Value = hash;
                        requested = (long) command.ExecuteScalar() == 1;
                    }

                    if (!requested)
                    {
                        if (content.Length > MaxUnrequestedBlocksVolume)
                        {
                            return;
                        }

                        TruncateUnrequestedBlocks(MaxUnrequestedBlocksVolume - content.Length);
                    }

                    using (SQLiteCommand command = new SQLiteCommand(
                        "insert into Blocks(Hash,Size,Received,Requested,Content)" +
                        "values (@Hash,@Size,@Received,@Requested,@Content)",
                        conn
                    ))
                    {
                        command.Parameters.Add("@Hash", DbType.Binary).Value = hash;
                        command.Parameters.Add("@Size", DbType.Int32).Value = content.Length;
                        // todo: check date format and maybe remove recieved column
                        command.Parameters.Add("@Received", DbType.DateTime).Value = SystemTime.UtcNow;
                        command.Parameters.Add("@Requested", DbType.Boolean).Value = requested;
                        command.Parameters.Add("@Content", DbType.Binary).Value = content;

                        command.ExecuteNonQuery();
                    }

                    tx.Commit();
                }
            }
        }

        private void TruncateUnrequestedBlocks(long maxVolume)
        {
            long? truncateBefore = null;
            using (SQLiteCommand command = new SQLiteCommand(
                "select Id, Size from Blocks where Requested=0 order by Id DESC",
                conn
            ))
            {
                long storedVolume = 0;
                using (var reader = command.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        storedVolume += reader.GetInt64(1);
                        if (storedVolume > maxVolume)
                        {
                            truncateBefore = reader.GetInt64(0);
                            break;
                        }
                    }
                }
            }

            if (truncateBefore != null)
            {
                using (SQLiteCommand command = new SQLiteCommand(
                    "delete from Blocks where Requested=0 and Id <= @truncateBefore",
                    conn
                ))
                {
                    command.Parameters.Add("@truncateBefore", DbType.Int64).Value = truncateBefore.Value;
                    command.ExecuteNonQuery();
                }
            }
        }

        public byte[] GetBlock(byte[] hash)
        {
            lock (monitor)
            {
                using (conn.BeginTransaction())
                {
                    using (SQLiteCommand command = new SQLiteCommand(
                        "select Content from Blocks where Hash=@Hash",
                        conn
                    ))
                    {
                        command.Parameters.Add("@Hash", DbType.Binary).Value = hash;
                        return (byte[]) command.ExecuteScalar();
                    }
                }
            }
        }

        public void UpdateRequests(string token, List<byte[]> hashes)
        {
            // todo: lock?
            List<byte[]> existingRequests = new List<byte[]>();

            using (var tx = conn.BeginTransaction())
            {
                using (SQLiteCommand command = new SQLiteCommand(
                    "select Hash from BlockRequests where Token=@Token",
                    conn
                ))
                {
                    command.Parameters.Add("@Token", DbType.String).Value = token;
                    using (var reader = command.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            existingRequests.Add((byte[]) reader[0]);
                        }
                    }
                }

                HashSet<byte[]> addedRequests = new HashSet<byte[]>(hashes, ByteArrayComparer.Instance);
                addedRequests.ExceptWith(existingRequests);

                HashSet<byte[]> removedRequests = new HashSet<byte[]>(existingRequests, ByteArrayComparer.Instance);
                removedRequests.ExceptWith(hashes);

                using (SQLiteCommand command = new SQLiteCommand(
                    "insert into BlockRequests(Token, Hash) values (@Token, @Hash)",
                    conn
                ))
                {
                    foreach (byte[] hash in addedRequests)
                    {
                        command.Parameters.Add("@Token", DbType.String).Value = token;
                        command.Parameters.Add("@Hash", DbType.Binary).Value = hash;
                        command.ExecuteNonQuery();
                        command.Parameters.Clear();
                    }
                }

                using (SQLiteCommand command = new SQLiteCommand(
                    "delete from BlockRequests where Token=@Token and Hash=@Hash",
                    conn
                ))
                {
                    foreach (byte[] hash in removedRequests)
                    {
                        command.Parameters.Add("@Token", DbType.String).Value = token;
                        command.Parameters.Add("@Hash", DbType.Binary).Value = hash;
                        command.ExecuteNonQuery();
                        command.Parameters.Clear();
                    }
                }

                using (SQLiteCommand command = new SQLiteCommand(
                    "update Blocks set Requested=1 where Hash=@Hash",
                    conn
                ))
                {
                    foreach (byte[] hash in addedRequests)
                    {
                        command.Parameters.Add("@Hash", DbType.Binary).Value = hash;
                        command.ExecuteNonQuery();
                        command.Parameters.Clear();
                    }
                }

                using (SQLiteCommand command = new SQLiteCommand(
                    "update Blocks set Requested=0 where Hash=@Hash" +
                    " and not exists(select 1 from BlockRequests R where R.Hash=@Hash)",
                    conn
                ))
                {
                    foreach (byte[] hash in removedRequests)
                    {
                        command.Parameters.Add("@Hash", DbType.Binary).Value = hash;
                        command.ExecuteNonQuery();
                        command.Parameters.Clear();
                    }
                }

                tx.Commit();
            }
        }

        public long GetUnrequestedVolume()
        {
            //todo: lock
            using (conn.BeginTransaction())
            {
                using (SQLiteCommand command = new SQLiteCommand(
                    "select ifnull(sum(Size), 0) from Blocks where Requested=0",
                    conn
                ))
                {
                    return (long) command.ExecuteScalar();
                }
            }
        }
    }
}