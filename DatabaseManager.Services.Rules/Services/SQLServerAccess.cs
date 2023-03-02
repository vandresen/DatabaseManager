using Dapper;
using Microsoft.Data.SqlClient;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DatabaseManager.Services.Rules.Services
{
    public class SQLServerAccess : IDatabaseAccess
    {
        private int _sqlTimeOut;

        public SQLServerAccess()
        {
            _sqlTimeOut = 1000;
        }
        public async Task DeleteData<T>(string sql, T parameters, string connectionString)
        {
            using IDbConnection cnn = new SqlConnection(connectionString);
            await cnn.ExecuteAsync(sql, parameters);
        }

        public void InsertWithUDT<T>(string storedProcedure, string parameterName, T collection, string connectionString)
        {
            SqlConnection conn = null;
            SqlParameter param = new SqlParameter();
            param.ParameterName = parameterName;
            param.SqlDbType = SqlDbType.Structured;
            param.Value = collection;
            param.Direction = ParameterDirection.Input;

            using (conn = new SqlConnection(connectionString))
            {
                SqlCommand sqlCmd = new SqlCommand(storedProcedure);
                conn.Open();
                sqlCmd.Connection = conn;
                sqlCmd.CommandTimeout = _sqlTimeOut;
                sqlCmd.CommandType = CommandType.StoredProcedure;
                sqlCmd.Parameters.Add(param);
                sqlCmd.ExecuteNonQuery();
            }
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
            await cnn.ExecuteAsync(storedProcedure, parameters, commandType: CommandType.StoredProcedure);
        }
    }
}
