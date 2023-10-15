using Dapper;
using Microsoft.Data.Sqlite;
using System.Data;

namespace DatabaseManager.Services.RulesSqlite.Services
{
    public class SqliteDataAccess : IDataAccess
    {
        public async Task ExecuteSQL(string sql, string connectionString)
        {
            using IDbConnection cnn = new SqliteConnection(connectionString);
            cnn.Open();
            await cnn.ExecuteAsync(sql);
            cnn.Close();
        }

        public async Task InsertUpdateData<T>(string sql, T parameters, string connectionString)
        {
            using IDbConnection cnn = new SqliteConnection(connectionString);
            await cnn.ExecuteAsync(sql, parameters);
        }

        public async Task<IEnumerable<T>> ReadData<T>(string sql, string connectionString)
        {
            using IDbConnection cnn = new SqliteConnection(connectionString);
            return await cnn.QueryAsync<T>(sql);
        }
    }
}
