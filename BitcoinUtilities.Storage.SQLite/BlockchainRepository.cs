using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SQLite;
using System.IO;
using System.Text;
using BitcoinUtilities.P2P;
using BitcoinUtilities.P2P.Primitives;

namespace BitcoinUtilities.Storage.SQLite
{
    /// <summary>
    /// A class that groups block-chain related database queries and manages creation and disposal of SQL-commands.
    /// </summary>
    internal class BlockchainRepository : IDisposable
    {
        private readonly Dictionary<string, SQLiteCommand> commands = new Dictionary<string, SQLiteCommand>();
        private readonly SQLiteConnection connection;

        public BlockchainRepository(SQLiteConnection connection)
        {
            this.connection = connection;
        }

        public void Dispose()
        {
            foreach (var command in commands.Values)
            {
                command.Dispose();
            }
        }

        private SQLiteCommand CreateCommand(string sql)
        {
            //todo: use separate class as cache for commands (it is not a responsibility of repositories)
            SQLiteCommand command;
            if (!commands.TryGetValue(sql, out command))
            {
                command = new SQLiteCommand(sql, connection);
                commands.Add(sql, command);
            }
            command.Parameters.Clear();
            return command;
        }

        public void InsertBlock(StoredBlock block)
        {
            var command = CreateCommand(
                "insert into Blocks(Header, Hash, Height, TotalWork, HasContent, IsInBestHeaderChain, IsInBestBlockChain)" +
                "values (@Header, @Hash, @Height, @TotalWork, @HasContent, @IsInBestHeaderChain, @IsInBestBlockChain)");

            command.Parameters.Add("@Header", DbType.Binary).Value = BitcoinStreamWriter.GetBytes(block.Header.Write);
            command.Parameters.Add("@Hash", DbType.Binary).Value = block.Hash;
            command.Parameters.Add("@Height", DbType.Int32).Value = block.Height;
            command.Parameters.Add("@TotalWork", DbType.Double).Value = block.TotalWork;
            command.Parameters.Add("@HasContent", DbType.Boolean).Value = block.HasContent;
            command.Parameters.Add("@IsInBestHeaderChain", DbType.Boolean).Value = block.IsInBestHeaderChain;
            command.Parameters.Add("@IsInBestBlockChain", DbType.Boolean).Value = block.IsInBestBlockChain;

            command.ExecuteNonQuery();
            //todo: block.Id = connection.LastInsertRowId;
        }

        public void UpdateBlock(StoredBlock block)
        {
            //todo: Header is not updated. Describe it in XMLDOC?
            var command = CreateCommand(
                "update Blocks" +
                "  set" +
                "    Height=@Height, " +
                "    TotalWork=@TotalWork, " +
                "    HasContent=@HasContent, " +
                "    IsInBestHeaderChain=@IsInBestHeaderChain, " +
                "    IsInBestBlockChain=@IsInBestBlockChain" +
                "  where Hash=@Hash");

            command.Parameters.Add("@Hash", DbType.Binary).Value = block.Hash;
            command.Parameters.Add("@Height", DbType.Int32).Value = block.Height;
            command.Parameters.Add("@TotalWork", DbType.Double).Value = block.TotalWork;
            command.Parameters.Add("@HasContent", DbType.Boolean).Value = block.HasContent;
            command.Parameters.Add("@IsInBestHeaderChain", DbType.Boolean).Value = block.IsInBestHeaderChain;
            command.Parameters.Add("@IsInBestBlockChain", DbType.Boolean).Value = block.IsInBestBlockChain;

            command.ExecuteNonQuery();
        }

        public StoredBlock FindBlockByHash(byte[] hash)
        {
            var command = CreateCommand($"select {GetBlockColumns("B")} from Blocks B where B.Hash=@Hash");

            command.Parameters.Add("@Hash", DbType.Binary).Value = hash;

            using (SQLiteDataReader reader = command.ExecuteReader())
            {
                if (!reader.Read())
                {
                    return null;
                }
                return ReadBlock(reader);
            }
        }

        public StoredBlock FindBestHeaderChain()
        {
            var command = CreateCommand($"select {GetBlockColumns("B")} from Blocks B where B.IsInBestHeaderChain=1 order by B.Height desc limit 1");

            using (SQLiteDataReader reader = command.ExecuteReader())
            {
                if (!reader.Read())
                {
                    return null;
                }
                return ReadBlock(reader);
            }
        }

        public StoredBlock GetLastBlockHeader()
        {
            var command = CreateCommand($"select {GetBlockColumns("B")} from Blocks B order by B.Height desc limit 1");
            using (SQLiteDataReader reader = command.ExecuteReader())
            {
                if (!reader.Read())
                {
                    return null;
                }
                return ReadBlock(reader);
            }
        }

        public List<StoredBlock> ReadHeadersWithHeight(int[] heights)
        {
            var command = CreateCommand(
                $"select {GetBlockColumns("B")} from Blocks B" +
                $" where B.IsInBestHeaderChain=1 and B.Height in ({GetInParameters("H", heights.Length)})" +
                $" order by B.Height asc");

            SetInParameters(command, "H", heights);

            List<StoredBlock> blocks = new List<StoredBlock>();

            using (SQLiteDataReader reader = command.ExecuteReader())
            {
                while (reader.Read())
                {
                    blocks.Add(ReadBlock(reader));
                }
            }

            return blocks;
        }

        public List<StoredBlock> GetOldestBlocksWithoutContent(int maxCount)
        {
            var command = CreateCommand(
                $"select {GetBlockColumns("B")} from Blocks B" +
                $" where B.IsInBestHeaderChain=1 and B.HasContent=0" +
                $" order by B.Height asc" +
                $" limit @maxCount");

            command.Parameters.Add("@maxCount", DbType.Int32).Value = maxCount;

            List<StoredBlock> blocks = new List<StoredBlock>();

            using (SQLiteDataReader reader = command.ExecuteReader())
            {
                while (reader.Read())
                {
                    blocks.Add(ReadBlock(reader));
                }
            }

            return blocks;
        }

        private string GetBlockColumns(string alias)
        {
            return $"{alias}.Header, {alias}.Height, {alias}.TotalWork, {alias}.HasContent, {alias}.IsInBestHeaderChain, {alias}.IsInBestBlockChain";
        }

        private StoredBlock ReadBlock(SQLiteDataReader reader)
        {
            int col = 0;

            byte[] headerBytes = ReadBytes(reader, col++);
            BlockHeader header = BlockHeader.Read(new BitcoinStreamReader(new MemoryStream(headerBytes)));
            StoredBlock block = new StoredBlock(header);

            //block.Hash - calculated from header
            block.Height = reader.GetInt32(col++);
            block.TotalWork = reader.GetDouble(col++);
            block.HasContent = reader.GetBoolean(col++);
            block.IsInBestHeaderChain = reader.GetBoolean(col++);
            block.IsInBestBlockChain = reader.GetBoolean(col++);

            return block;
        }

        //todo: move to utils?
        private string GetInParameters(string parameterPrefix, int valuesCount)
        {
            StringBuilder sb = new StringBuilder();
            for (int i = 0; i < valuesCount; i++)
            {
                if (i > 0)
                {
                    sb.Append(", ");
                }
                sb.Append($"@{parameterPrefix}{i}");
            }
            return sb.ToString();
        }

        //todo: move to utils?
        private void SetInParameters(SQLiteCommand command, string parameterPrefix, int[] values)
        {
            for (int i = 0; i < values.Length; i++)
            {
                command.Parameters.Add($"@{parameterPrefix}{i}", DbType.Int32).Value = values[i];
            }
        }

        //todo: move to utils?
        private byte[] ReadBytes(SQLiteDataReader reader, int col)
        {
            return (byte[]) reader[col];
        }
    }
}