using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SQLite;
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
    }
}