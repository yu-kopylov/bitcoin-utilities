using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SQLite;
using System.IO;
using System.Reflection;
using System.Text;

namespace BitcoinUtilities.Node.Services
{
    public static class SqliteUtils
    {
        public static void CheckSchema(SQLiteConnection conn, string tableName, Type resourceType, string resourceName)
        {
            using (var tx = conn.BeginTransaction())
            {
                long hasTable;

                using (SQLiteCommand command = new SQLiteCommand(
                    "select count(*) from sqlite_master WHERE type='table' AND name=@tableName",
                    conn
                ))
                {
                    command.Parameters.Add("tableName", DbType.String).Value = tableName;
                    hasTable = (long) command.ExecuteScalar();
                }

                if (hasTable == 0)
                {
                    CreateSchema(conn, resourceType, resourceName);
                }

                tx.Commit();
            }
        }

        private static void CreateSchema(SQLiteConnection conn, Type resourceType, string resourceName)
        {
            string createSchemaSql;
            Assembly schemaAssembly = resourceType.Assembly;
            string schemaResourceName = $"{resourceType.Namespace}.{resourceName}";
            using (var stream = schemaAssembly.GetManifestResourceStream(schemaResourceName))
            {
                using (StreamReader reader = new StreamReader(stream, Encoding.UTF8))
                {
                    createSchemaSql = reader.ReadToEnd();
                }
            }

            using (SQLiteCommand command = new SQLiteCommand(createSchemaSql, conn))
            {
                command.ExecuteNonQuery();
            }
        }

        public static string GetInParameters(string parameterPrefix, int valuesCount)
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

        public static void SetInParameters<T>(SQLiteParameterCollection parameters, string parameterPrefix, DbType parameterType, IReadOnlyCollection<T> values)
        {
            int index = 0;
            foreach (T value in values)
            {
                parameters.Add($"@{parameterPrefix}{index}", parameterType).Value = value;
                index++;
            }
        }

        public static int ExecuteNonQuery(this SQLiteConnection conn, string sql, params Action<SQLiteParameterCollection>[] parameterSetters)
        {
            using (SQLiteCommand command = new SQLiteCommand(sql, conn))
            {
                foreach (var parameterSetter in parameterSetters)
                {
                    parameterSetter(command.Parameters);
                }

                return command.ExecuteNonQuery();
            }
        }
    }
}