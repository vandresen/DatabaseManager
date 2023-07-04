using Microsoft.Data.SqlClient;
using System.Data;

namespace DatabaseManager.Services.IndexSqlite.Services
{
    public class DBDataAccess : IDataAccess
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

        public Task ExecuteSQL(string sql, string connectionString)
        {
            throw new NotImplementedException();
        }

        public async Task<DataTable> GetDataTable(string select, string query, string dataType)
        {
            SqlConnection conn = null;
            string sql = select + query;
            DataTable result = new DataTable();
            using (conn = new SqlConnection(_connectionString))
            {
                conn.Open();
                SqlCommand cmd = new SqlCommand(sql, conn);
                cmd.CommandTimeout = _sqlTimeOut;
                SqlDataReader dr = cmd.ExecuteReader();
                result.Load(dr);
                dr.Close();
            }
            return result;
        }

        public Task<IEnumerable<T>> LoadData<T, U>(string storedProcedure, U parameters, string connectionString)
        {
            throw new NotImplementedException();
        }

        public void OpenConnection(ConnectParameters source, ConnectParameters target)
        {
            _connectionString = source.ConnectionString;
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
    }
}
