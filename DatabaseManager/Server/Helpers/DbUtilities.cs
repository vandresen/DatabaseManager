using DatabaseManager.Shared;
using Microsoft.Data.SqlClient;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Threading.Tasks;

namespace DatabaseManager.Server.Helpers
{
    public class DbUtilities
    {
        private SqlConnection sqlCn = null;
        private int _sqlTimeOut;

        public DbUtilities()
        {
            _sqlTimeOut = 1000;
        }

        public void OpenConnection(ConnectParameters connection)
        {
            string connectionString = GetConnectionString(connection);
            sqlCn = new SqlConnection();
            sqlCn.ConnectionString = connectionString;
            sqlCn.Open();
        }

        public void CloseConnection()
        {
            sqlCn.Close();
        }

        public void SQLExecute(string sql)
        {
            using (SqlCommand cmd = new SqlCommand(sql, this.sqlCn))
            {
                try
                {
                    cmd.CommandTimeout = _sqlTimeOut;
                    cmd.ExecuteNonQuery();
                }
                catch (SqlException ex)
                {
                    Exception error = new Exception("SQLExecute: Error executing SQL: ", ex);
                    throw error;
                }

            }
        }

        public void DBDelete(string table, string query = "")
        {
            int rows;
            string sql = "Delete from " + table + " " + query;
            using (SqlCommand cmd = new SqlCommand(sql, this.sqlCn))
            {
                try
                {
                    cmd.CommandTimeout = _sqlTimeOut;
                    rows = cmd.ExecuteNonQuery();
                }
                catch (SqlException ex)
                {
                    Exception error = new Exception($"Sorry! Error deleting from table {table}: {ex}");
                    throw error;
                }
            }
        }

        public void DBInsert(string strInsert, string strValue, string strQuery)
        {
            string sql = strInsert + strValue + strQuery;
            using (SqlCommand cmd = new SqlCommand(sql, this.sqlCn))
            {
                try
                {
                    cmd.CommandTimeout = _sqlTimeOut;
                    cmd.ExecuteNonQuery();
                }
                catch (SqlException ex)
                {
                    Exception error = new Exception("Error inserting into table: ", ex);
                    throw error;
                }
            }
        }

        public static string GetConnectionString(ConnectParameters connection)
        {
            string source = $"Source={connection.DatabaseServer};";
            string database = $"Initial Catalog ={connection.Database};";
            string timeout = "Connection Timeout=180";
            string persistSecurity = "Persist Security Info=False;";
            string multipleActive = "MultipleActiveResultSets=True;";
            string integratedSecurity = "";
            string user = "";
            //Encryption is currently not used, more testing later
            string encryption = "Encrypt=True;TrustServerCertificate=False;";
            if (!string.IsNullOrWhiteSpace(connection.DatabaseUser))
                user = $"User ID={connection.DatabaseUser};";
            else
                integratedSecurity = "Integrated Security=True;";
            string password = "";
            if (!string.IsNullOrWhiteSpace(connection.DatabasePassword)) password = $"Password={connection.DatabasePassword};";

            string cnStr = "Data " + source + persistSecurity + database +
                user + password + integratedSecurity + multipleActive;

            cnStr = cnStr + timeout;

            return cnStr;
        }

        public DataTable GetDataTable(string select, string query)
        {
            DataTable dt = new DataTable();

            string sql = string.Format(select + query);
            using (SqlCommand cmd = new SqlCommand(sql, this.sqlCn))
            {
                try
                {
                    cmd.CommandTimeout = _sqlTimeOut;
                    SqlDataReader dr = cmd.ExecuteReader();
                    dt.Load(dr);
                    dr.Close();
                }
                catch (SqlException ex)
                {
                    Exception error = new Exception("Sorry! Error getting data: ", ex);
                    throw error;
                }
            }
            return dt;
        }

        public string GetUsername()
        {
            string sql = "Select ORIGINAL_LOGIN()";
            string userName = "UNKNOWN";
            using (SqlCommand cmd = new SqlCommand(sql, this.sqlCn))
            {
                try
                {
                    cmd.CommandTimeout = _sqlTimeOut;
                    userName = (string)cmd.ExecuteScalar();
                }
                catch (SqlException ex)
                {
                    Exception error = new Exception("Error getting sequence: ", ex);
                    throw error;
                }

            }
            return userName;
        }

        public void InsertDataObject(string jsonData, string dataType)
        {
            string sql = "spInsert" + dataType;
            string paramName = "@json";
            using (SqlCommand cmd = new SqlCommand(sql, this.sqlCn))
            {
                try
                {
                    cmd.CommandTimeout = _sqlTimeOut;
                    cmd.CommandType = CommandType.StoredProcedure;
                    cmd.Parameters.AddWithValue(paramName, jsonData);
                    cmd.ExecuteNonQuery();
                }
                catch (Exception ex)
                {
                    Exception error = new Exception("Sorry! Error inserting data object: ", ex);
                    throw error;
                }

            }
        }

        public void UpdateDataObject(string jsonData, string dataType)
        {
            string sql = "spUpdate" + dataType;
            string paramName = "@json";
            using (SqlCommand cmd = new SqlCommand(sql, this.sqlCn))
            {
                try
                {
                    cmd.CommandTimeout = _sqlTimeOut;
                    cmd.CommandType = CommandType.StoredProcedure;
                    cmd.Parameters.AddWithValue(paramName, jsonData);
                    cmd.ExecuteNonQuery();
                }
                catch (Exception ex)
                {
                    Exception error = new Exception($"Sorry! Error updating data object", ex);
                    throw error;
                }

            }
        }

        public int InsertIndex(int parentId, string dataName, string dataType, string dataKey,
            string jsonData, double latitude, double longitude)
        {
            int id = -1;
            string sql;
            Boolean nullLocation = (latitude == -99999.0 | longitude == -99999.0);
            Boolean zeroLocation = (latitude == 0.0 & longitude == 0.0);
            Boolean addLocationParameters = false;
            if (parentId == -1)
            {
                sql = "spCreateIndex";
            }
            else if (nullLocation || zeroLocation)
            {
                sql = "spAddIndex";
            }
            
            else
            {
                sql = "spAddIndexWithLocation";
                addLocationParameters = true;
            }

            using (SqlCommand cmd = new SqlCommand(sql, this.sqlCn))
            {
                try
                {
                    cmd.CommandTimeout = _sqlTimeOut;
                    cmd.CommandType = CommandType.StoredProcedure;
                    if (parentId != -1)
                    {
                        cmd.Parameters.AddWithValue("@parentid", parentId);
                        cmd.Parameters.AddWithValue("@d_name", dataName);
                        cmd.Parameters.AddWithValue("@type", dataType);
                        cmd.Parameters.AddWithValue("@datakey", dataKey);
                        cmd.Parameters.AddWithValue("@jsondataobject", jsonData);
                    }
                    if (addLocationParameters)
                    {
                        cmd.Parameters.AddWithValue("@latitude", latitude);
                        cmd.Parameters.AddWithValue("@longitude", longitude);
                    }
                    object value = cmd.ExecuteScalar();
                    if (value != null)
                    {
                        id = Convert.ToInt32(value);
                    }
                }
                catch (Exception ex)
                {
                    Exception error = new Exception("Sorry! Error inserting index: ", ex);
                    string strError = "ParameterInsertQcIndex:" + error.ToString();
                    throw error;
                }

            }
            return id;
        }
    }
}
