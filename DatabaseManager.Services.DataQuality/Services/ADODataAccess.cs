using Microsoft.Data.SqlClient;
using System.Data;

namespace DatabaseManager.Services.DataQuality.Services
{
    public class ADODataAccess : IDataAccess
    {
        public DataTable GetDataTable(string sql, string connectionString)
        {
            SqlConnection conn = null;
            DataTable result = new DataTable();
            using (conn = new SqlConnection(connectionString))
            {
                conn.Open();
                SqlCommand cmd = new SqlCommand(sql, conn);
                SqlDataReader dr = cmd.ExecuteReader();
                result.Load(dr);
                dr.Close();
            }
            return result;
        }
    }
}
