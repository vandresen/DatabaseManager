using Microsoft.Data.SqlClient;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DatabaseManager.Common.DBAccess
{
    public class ADODataAccess: IADODataAccess
    {
        private int _sqlTimeOut;

        public ADODataAccess()
        {
            _sqlTimeOut = 1000;
        }

        public void Delete(string table, string connectionString)
        {
            SqlConnection conn = null;
            string storedProcedure = "dbo.spFastDelete";
            string paramName = "@TableName";
            using (conn = new SqlConnection(connectionString))
            {
                SqlCommand sqlCmd = new SqlCommand(storedProcedure);
                conn.Open();
                sqlCmd.Connection = conn;
                sqlCmd.CommandType = CommandType.StoredProcedure;
                sqlCmd.Parameters.AddWithValue(paramName, table);
                sqlCmd.ExecuteNonQuery();
            }
        }

        public void ExecuteSQL(string sql, string connectionString)
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

        public DataTable GetDataTable(string sql, string connectionString)
        {
            SqlConnection conn = null;
            DataTable result = new DataTable();
            using (conn = new SqlConnection(connectionString))
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

        public async Task InsertWithUDT<T>(string storedProcedure, string parameterName, T collection, string connectionString)
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
                sqlCmd.CommandType = CommandType.StoredProcedure;
                sqlCmd.Parameters.Add(param);
                sqlCmd.ExecuteNonQuery();
            }
        }
    }
}
