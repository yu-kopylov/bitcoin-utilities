using System;
using System.Data;
using System.Data.SQLite;
using System.IO;

namespace BitcoinUtilities.Node.Services.Blocks
{
    internal class BlockStorage : IDisposable
    {
        private const string BlocksFilename = "blocks2.db";

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
            using (var tx = conn.BeginTransaction())
            {
                bool requested;

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
}