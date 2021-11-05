using Dapper;
using DatabaseManager.Common.Entities;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Linq;
using System.Text;

namespace DatabaseManager.Common.Helpers
{
    public class CommonDbUtilities
    {
        public static ColumnProperties GetColumnSchema(DbUtilities dbConn, string sql)
        {
            ColumnProperties colProps = new ColumnProperties();
            string attributeType = "";
            string table = Common.GetTable(sql);
            string select = $"Select * from INFORMATION_SCHEMA.COLUMNS ";
            string query = $" where TABLE_NAME = '{table}'";
            DataTable dt = dbConn.GetDataTable(select, query);
            if (dt.Rows.Count == 0)
            {
                throw new ArgumentException("Table does not exist");
            }

            string[] sqlAttributes = Common.GetAttributes(sql);
            dt.CaseSensitive = false;

            foreach (string attribute in sqlAttributes)
            {
                string attributeIndex = attribute.Trim();
                query = $"COLUMN_NAME = '{attributeIndex}'";
                DataRow[] dtRows = dt.Select(query);
                if (dtRows.Length == 1)
                {
                    attributeType = dtRows[0]["DATA_TYPE"].ToString();
                    if (attributeType == "nvarchar")
                    {
                        string charLength = dtRows[0]["CHARACTER_MAXIMUM_LENGTH"].ToString();
                        attributeType = attributeType + "(" + charLength + ")";
                    }
                    else if (attributeType == "numeric")
                    {
                        string numericPrecision = dtRows[0]["NUMERIC_PRECISION"].ToString();
                        string numericScale = dtRows[0]["NUMERIC_SCALE"].ToString();
                        attributeType = attributeType + "(" + numericPrecision + "," + numericScale + ")";
                    }
                }
                else
                {
                    //Console.WriteLine("Warning: attribute not found");
                }

                colProps[attributeIndex] = attributeType;
            }

            return colProps;
        }

        public static string GetUsername(string connectionString)
        {
            string userName = "UNKNOWN";
            using (IDbConnection cnn = new SqlConnection(connectionString))
            {
                string sql = @"select stuff(suser_sname(), 1, charindex('\', suser_sname()), '') as UserName";
                var vidar = cnn.Query(sql);
                userName = vidar.Select(s => s.UserName).FirstOrDefault();
            }
            return userName;
        }
    }
}
