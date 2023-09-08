using Dapper;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore.Infrastructure;
using System.Data;
using static Microsoft.EntityFrameworkCore.DbLoggerCategory.Database;

namespace DatabaseManager.Services.IndexSqlite.Services
{
    public class SqliteDataAccess : IDataAccess
    {
        public SqliteDataAccess()
        {

        }

        public Task<T> Count<T, U>(string sql, U parameters, string connectionString)
        {
            throw new NotImplementedException();
        }

        public async Task DeleteDataSQL<T>(string sql, T parameters, string connectionString)
        {
            using IDbConnection cnn = new SqliteConnection(connectionString);
            SpatialiteLoader.Load((System.Data.Common.DbConnection)cnn);
            if (parameters == null)
            {
                await cnn.ExecuteAsync(sql);
            }
            else
            {
                await cnn.ExecuteAsync(sql, parameters);
            }
        }

        public async Task ExecuteSQL(string sql, string connectionString)
        {
            
            using IDbConnection cnn = new SqliteConnection(connectionString);
            cnn.Open();
            SpatialiteLoader.Load((System.Data.Common.DbConnection)cnn);
            await cnn.ExecuteAsync(sql);
            cnn.Close();
        }

        public Task<DataTable> GetDataTable(string select, string query, string dataType)
        {
            throw new NotImplementedException();
        }

        public Task<IEnumerable<T>> LoadData<T, U>(string storedProcedure, U parameters, string connectionString)
        {
            throw new NotImplementedException();
        }

        public void OpenConnection(ConnectParameters source, ConnectParameters target)
        {
            throw new NotImplementedException();
        }

        public async Task<IEnumerable<T>> ReadData<T>(string sql, string connectionString)
        {
            using IDbConnection cnn = new SqliteConnection(connectionString);
            SpatialiteLoader.Load((System.Data.Common.DbConnection)cnn);
            return await cnn.QueryAsync<T>(sql);
        }

        public Task SaveData<T>(string storedProcedure, T parameters, string connectionString)
        {
            throw new NotImplementedException();
        }

        public async Task SaveDataSQL<T>(string sql, T parameters, string connectionString)
        {
            using IDbConnection cnn = new SqliteConnection(connectionString);
            SpatialiteLoader.Load((System.Data.Common.DbConnection)cnn);
            await cnn.ExecuteAsync(sql, parameters);
        }
    }
}
