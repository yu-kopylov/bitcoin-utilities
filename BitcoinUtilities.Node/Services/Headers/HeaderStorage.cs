using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SQLite;
using BitcoinUtilities.P2P;
using BitcoinUtilities.P2P.Primitives;
using NLog;

namespace BitcoinUtilities.Node.Services.Headers
{
    public class HeaderStorage : IDisposable
    {
        private static readonly ILogger logger = LogManager.GetCurrentClassLogger();

        private readonly SQLiteConnection conn;

        private HeaderStorage(SQLiteConnection conn)
        {
            this.conn = conn;
        }

        public void Dispose()
        {
            conn.Close();
            conn.Dispose();
        }

        public static HeaderStorage Open(string filename)
        {
            SQLiteConnectionStringBuilder connStrBuilder = new SQLiteConnectionStringBuilder();
            connStrBuilder.DataSource = filename;
            connStrBuilder.JournalMode = SQLiteJournalModeEnum.Wal;

            var conn = new SQLiteConnection(connStrBuilder.ConnectionString);
            conn.Open();

            try
            {
                SqliteUtils.CheckSchema(conn, "Headers", typeof(HeaderStorage), "headers-create.sql");
            }
            catch (Exception ex)
            {
                logger.Error(ex, $"Failed to verify schema in '{filename}'.");
                conn.Close();
                conn.Dispose();
                throw;
            }

            return new HeaderStorage(conn);
        }

        public List<DbHeader> ReadAll()
        {
            List<DbHeader> res = new List<DbHeader>();

            using (var tx = conn.BeginTransaction())
            {
                using (SQLiteCommand command = new SQLiteCommand($"select Header, Hash, Height, TotalWork from Headers order by Height asc", conn))
                {
                    using (var reader = command.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            int col = 0;

                            BlockHeader header = BitcoinStreamReader.FromBytes((byte[]) reader[col++], BlockHeader.Read);
                            byte[] hash = (byte[]) reader[col++];
                            int height = reader.GetInt32(col++);
                            double totalWork = reader.GetDouble(col++);

                            res.Add(new DbHeader(header, hash, height, totalWork));
                        }
                    }
                }

                tx.Commit();
            }

            return res;
        }

        public void AddHeaders(IReadOnlyCollection<DbHeader> headers)
        {
            using (var tx = conn.BeginTransaction())
            {
                using (SQLiteCommand command = new SQLiteCommand(
                    "insert into Headers (Header, Hash, Height, TotalWork)" +
                    "values (@Header, @Hash, @Height, @TotalWork)",
                    conn))
                {
                    foreach (var header in headers)
                    {
                        command.Parameters.Add("@Header", DbType.Binary).Value = BitcoinStreamWriter.GetBytes(header.Header.Write);
                        command.Parameters.Add("@Hash", DbType.Binary).Value = header.Hash;
                        command.Parameters.Add("@Height", DbType.Int32).Value = header.Height;
                        command.Parameters.Add("@TotalWork", DbType.Double).Value = header.TotalWork;

                        command.ExecuteNonQuery();
                        command.Parameters.Clear();
                    }
                }

                tx.Commit();
            }
        }
    }
}