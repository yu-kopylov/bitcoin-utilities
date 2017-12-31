using System;
using System.Data.SQLite;
using System.IO;

namespace BitcoinUtilities.Node.Services.Blocks
{
    public class BlockStorage : IDisposable
    {
        private readonly SQLiteConnection conn;
        private const string BlocksFilename = "blocks2.db";

        public BlockStorage(SQLiteConnection conn)
        {
            this.conn = conn;
        }

        public void Dispose()
        {
            conn.Close();
            conn.Dispose();
        }

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
    }
}