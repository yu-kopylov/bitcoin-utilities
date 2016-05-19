using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SQLite;
using System.Text;
using BitcoinUtilities.Storage.Models;

namespace BitcoinUtilities.Storage.Sql
{
    /// <summary>
    /// A class that groups block-chain related database queries and manages creation and disposal of SQL-commands.
    /// </summary>
    public class BlockChainRepository : IDisposable
    {
        private readonly Dictionary<string, SQLiteCommand> commands = new Dictionary<string, SQLiteCommand>();
        private readonly SQLiteConnection connection;

        public BlockChainRepository(SQLiteConnection connection)
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
            SQLiteCommand command;
            if (!commands.TryGetValue(sql, out command))
            {
                command = new SQLiteCommand(sql, connection);
                commands.Add(sql, command);
            }
            command.Parameters.Clear();
            return command;
        }

        public void InsertBlock(Block block)
        {
            var command = CreateCommand("insert into Blocks(Hash, Height, Header) values (@Hash, @Height, @Header)");

            command.Parameters.Add("@Hash", DbType.Binary).Value = block.Hash;
            command.Parameters.Add("@Height", DbType.Int32).Value = block.Height;
            command.Parameters.Add("@Header", DbType.Binary).Value = block.Header;

            command.ExecuteNonQuery();
            block.Id = connection.LastInsertRowId;
        }

        public void InsertBinaryData(BinaryData data)
        {
            var command = CreateCommand("insert into BinaryData(Data) values (@Data)");

            command.Parameters.Add("@Data", DbType.Binary).Value = data.Data;

            command.ExecuteNonQuery();
            data.Id = connection.LastInsertRowId;
        }

        public Block GetLastBlockHeader()
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

        public List<Block> ReadHeadersWithHeight(int[] heights)
        {
            var command = CreateCommand(
                $"select {GetBlockColumns("B")} from Blocks B" +
                $" where B.Height in ({GetInParameters("H", heights.Length)})" +
                $" order by B.Height asc");

            SetInParameters(command, "H", heights);

            List<Block> blocks = new List<Block>();

            using (SQLiteDataReader reader = command.ExecuteReader())
            {
                while(reader.Read())
                {
                    blocks.Add(ReadBlock(reader));
                }
            }

            return blocks;
        }

        private string GetBlockColumns(string alias)
        {
            return $"{alias}.Id, {alias}.Hash, {alias}.Height, {alias}.Header";
        }

        private Block ReadBlock(SQLiteDataReader reader)
        {
            Block block = new Block();

            int col = 0;

            block.Id = reader.GetInt64(col++);
            block.Hash = ReadBytes(reader, col++);
            block.Height = reader.GetInt32(col++);
            block.Header = ReadBytes(reader, col++);

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