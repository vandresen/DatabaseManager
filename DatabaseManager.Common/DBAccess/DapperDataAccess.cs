using Dapper;
using Microsoft.Data.SqlClient;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DatabaseManager.Common.DBAccess
{
    public class DapperDataAccess : IDapperDataAccess
    {
        public DapperDataAccess()
        {
        }

        public async Task<IEnumerable<T>> LoadData<T, U>(string storedProcedure, U parameters, string connectionString)
        {
            using IDbConnection cnn = new SqlConnection(connectionString);
            return await cnn.QueryAsync<T>(storedProcedure, parameters, commandType: CommandType.StoredProcedure);
        }

        public async Task SaveData<T>(string storedProcedure, T parameters, string connectionString)
        {
            using IDbConnection cnn = new SqlConnection(connectionString);
            await cnn.ExecuteAsync(storedProcedure, parameters, commandType: CommandType.StoredProcedure);
        }

        public async Task SaveDataSQL<T>(string sql, T parameters, string connectionString)
        {
            using IDbConnection cnn = new SqlConnection(connectionString);
            await cnn.ExecuteAsync(sql, parameters);
        }

        public async Task<IEnumerable<T>> ReadData<T>(string sql, string connectionString)
        {
            using IDbConnection cnn = new SqlConnection(connectionString);
            return await cnn.QueryAsync<T>(sql);
        }

        public async Task<T> Count<T, U>(string sql, U parameters, string connectionString)
        {
            using IDbConnection cnn = new SqlConnection(connectionString);
            var count = await cnn.ExecuteScalarAsync<T>(sql, parameters);
            return count;
        }

        public async Task<T> SaveDataScalar<T, U>(string storedProcedure, U parameters, string connectionString)
        {
            using IDbConnection cnn = new SqlConnection(connectionString);
            var result = await cnn.ExecuteScalarAsync<T>(storedProcedure, parameters, commandType: CommandType.StoredProcedure);
            return result;
        }

        public async Task DeleteData<T>(string sql, T parameters, string connectionString)
        {
            using IDbConnection cnn = new SqlConnection(connectionString);
            await cnn.ExecuteAsync(sql, parameters);
        }
    }
}
