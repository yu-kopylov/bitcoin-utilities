using System;
using System.Collections.Generic;
using System.Data.SQLite;

namespace BitcoinUtilities.Storage.SQLite
{
    internal class CommandCache : IDisposable
    {
        private readonly SQLiteConnection connection;

        private readonly Dictionary<string, SQLiteCommand> commands = new Dictionary<string, SQLiteCommand>();

        public CommandCache(SQLiteConnection connection)
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

        public SQLiteCommand CreateCommand(string sql)
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
    }
}