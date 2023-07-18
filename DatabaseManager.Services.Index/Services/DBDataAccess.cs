using DatabaseManager.Services.Index.Models;
using Microsoft.Data.SqlClient;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlTypes;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DatabaseManager.Services.Index.Services
{
    internal class DBDataAccess : IDataAccess
    {
        private string _connectionString;
        private int _sqlTimeOut;

        public DBDataAccess()
        {
            _sqlTimeOut = 1000;
        }

        public Task<T> Count<T, U>(string sql, U parameters, string connectionString)
        {
            throw new NotImplementedException();
        }

        public Task DeleteDataSQL<T>(string sql, T parameters, string connectionString)
        {
            throw new NotImplementedException();
        }

        public async Task ExecuteSQL(string sql, string connectionString)
        {
            SqlConnection conn = null;
            using (conn = new SqlConnection(connectionString))
            {
                conn.Open();
                SqlCommand cmd = new SqlCommand(sql, conn);
                cmd.CommandTimeout = _sqlTimeOut;
                cmd.ExecuteNonQuery();
                conn.Close();
            }
        }

        public Task<DataTable> GetDataTable(string select, string query, string dataType)
        {
            throw new NotImplementedException();
        }

        public async Task InsertWithUDT<T>(string storedProcedure, string parameterName, T collection, string connectionString)
        {
            using (SqlConnection conn = new SqlConnection(connectionString))
            {
                conn.Open();
                using (SqlCommand command = conn.CreateCommand())
                {
                    command.CommandTimeout = _sqlTimeOut;
                    command.CommandType = CommandType.StoredProcedure;
                    command.CommandText = storedProcedure;
                    SqlParameter parameter = command.Parameters.AddWithValue("@TempTable", collection);
                    parameter.SqlDbType = SqlDbType.Structured;
                    parameter.TypeName = "dbo.UDIndexTable";
                    command.ExecuteNonQuery();
                }
            }
        }

        public Task<IEnumerable<T>> LoadData<T, U>(string storedProcedure, U parameters, string connectionString)
        {
            throw new NotImplementedException();
        }

        public void OpenConnection(ConnectParametersDto source, ConnectParametersDto target)
        {
            throw new NotImplementedException();
        }

        public Task<IEnumerable<T>> ReadData<T>(string sql, string connectionString)
        {
            throw new NotImplementedException();
        }

        public Task SaveData<T>(string storedProcedure, T parameters, string connectionString)
        {
            throw new NotImplementedException();
        }

        public Task SaveDataSQL<T>(string sql, T parameters, string connectionString)
        {
            throw new NotImplementedException();
        }

        public void WakeUpDatabase(string connectionString)
        {
            SqlConnection conn = null;
            using (conn = new SqlConnection(connectionString))
            {
                conn.Open();
                conn.Close();
            }
        }
    }
}
