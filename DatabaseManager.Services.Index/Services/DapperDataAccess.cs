using Dapper;
using Microsoft.Data.SqlClient;
using System.Data;

namespace DatabaseManager.Services.Index.Services
{
    public class DapperDataAccess : IDapperDataAccess
    {
        public Task<T> Count<T, U>(string sql, U parameters, string connectionString)
        {
            throw new NotImplementedException();
        }

        public async Task<IEnumerable<T>> LoadData<T, U>(string storedProcedure, U parameters, string connectionString)
        {
            using IDbConnection cnn = new SqlConnection(connectionString);
            return await cnn.QueryAsync<T>(storedProcedure, parameters, commandType: CommandType.StoredProcedure);
        }

        public async Task<IEnumerable<T>> ReadData<T>(string sql, string connectionString)
        {
            using IDbConnection cnn = new SqlConnection(connectionString);
            return await cnn.QueryAsync<T>(sql);
        }

        public async Task SaveData<T>(string storedProcedure, T parameters, string connectionString)
        {
            using IDbConnection cnn = new SqlConnection(connectionString);
            cnn.Execute(storedProcedure, parameters, commandType: CommandType.StoredProcedure);
        }

        public Task SaveDataSQL<T>(string sql, T parameters, string connectionString)
        {
            throw new NotImplementedException();
        }
    }
}
