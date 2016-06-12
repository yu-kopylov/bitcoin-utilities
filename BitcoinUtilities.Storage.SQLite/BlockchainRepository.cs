using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SQLite;
using System.IO;
using System.Linq;
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
        private readonly CommandCache commandCache;

        public BlockchainRepository(SQLiteConnection connection)
        {
            this.commandCache = new CommandCache(connection);
        }

        public void Dispose()
        {
            commandCache.Dispose();
        }

        public void InsertBlock(StoredBlock block)
        {
            var command = commandCache.CreateCommand(
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

        public void AddBlockContent(byte[] hash, byte[] content)
        {
            StoredBlock block = FindBlockByHash(hash);
            if (block == null)
            {
                //todo: use better approach?
                throw new IOException($"Block not found in storage (hash: {BitConverter.ToString(hash)}).");
            }
            //todo: what if there is content already?
            block.HasContent = true;
            UpdateBlock(block);

            var command = CreateCommand("insert into BlockContents (Hash, Content) values (@Hash, @Content)");
            command.Parameters.Add("@Hash", DbType.Binary).Value = hash;
            command.Parameters.Add("@Content", DbType.Binary).Value = content;
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

        public StoredBlock FindFirst(BlockSelector selector)
        {
            string whereClause = GetWhereClause("B", selector);
            string orderByClause = GetOrderByClause("B", selector);

            var command = CreateCommand($"select {GetBlockColumns("B")} from Blocks B {whereClause} {orderByClause} limit 1");

            SetParameters(command, selector);

            using (SQLiteDataReader reader = command.ExecuteReader())
            {
                if (!reader.Read())
                {
                    return null;
                }
                return ReadBlock(reader);
            }
        }

        public List<StoredBlock> Find(BlockSelector selector, int count)
        {
            string whereClause = GetWhereClause("B", selector);
            string orderByClause = GetOrderByClause("B", selector);

            var command = CreateCommand($"select {GetBlockColumns("B")} from Blocks B {whereClause} {orderByClause} limit @limit");

            SetParameters(command, selector);
            command.Parameters.Add("@limit", DbType.Int32).Value = count;

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

        private string GetWhereClause(string blocksAlias, BlockSelector selector)
        {
            List<string> criteria = new List<string>();
            if (selector.Heights != null)
            {
                criteria.Add($"{blocksAlias}.Height in ({GetInParameters("H", selector.Heights.Length)})");
            }
            if (selector.HasContent != null)
            {
                criteria.Add($"{blocksAlias}.HasContent=@HasContent");
            }
            if (selector.IsInBestHeaderChain != null)
            {
                criteria.Add($"{blocksAlias}.IsInBestHeaderChain=@IsInBestHeaderChain");
            }
            if (selector.IsInBestBlockChain != null)
            {
                criteria.Add($"{blocksAlias}.IsInBestBlockChain=@IsInBestBlockChain");
            }
            if (!criteria.Any())
            {
                return "";
            }
            return "where " + string.Join(" AND ", criteria);
        }

        private void SetParameters(SQLiteCommand command, BlockSelector selector)
        {
            if (selector.Heights != null)
            {
                SetInParameters(command, "H", selector.Heights);
            }
            if (selector.HasContent != null)
            {
                command.Parameters.Add("@HasContent", DbType.Boolean).Value = selector.HasContent;
            }
            if (selector.IsInBestHeaderChain != null)
            {
                command.Parameters.Add("@IsInBestHeaderChain", DbType.Boolean).Value = selector.IsInBestHeaderChain;
            }
            if (selector.IsInBestBlockChain != null)
            {
                command.Parameters.Add("@IsInBestBlockChain", DbType.Boolean).Value = selector.IsInBestBlockChain;
            }
        }

        private string GetOrderByClause(string blocksAlias, BlockSelector selector)
        {
            if (selector.Order == null || selector.Direction == null)
            {
                throw new ArgumentException("The sort order is not defined.", nameof(selector));
            }
            return $"order by {blocksAlias}.{selector.Order} {selector.Direction}";
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

        private SQLiteCommand CreateCommand(string sql)
        {
            return commandCache.CreateCommand(sql);
        }
    }
}