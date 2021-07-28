using DatabaseManager.Shared;
using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Text;

namespace DatabaseManager.Common.Helpers
{
    public class DatabaseLoader
    {
        public DatabaseLoader()
        {

        }

        public void CopyTable(TransferParameters transferParameters, string sourceCnStr, string destCnStr)
        {
            string table = transferParameters.Table;
            SqlConnection sourceConn = new SqlConnection(sourceCnStr);
            SqlConnection destinationConn = new SqlConnection(destCnStr);
            try
            {
                sourceConn.Open();
                destinationConn.Open();
                BulkCopy(sourceConn, destinationConn, transferParameters);

            }
            catch (Exception ex)
            {
                Exception error = new Exception($"Sorry! Error copying table: {table}; {ex}");
                throw error;
            }
            finally
            {
                sourceConn.Close();
                destinationConn.Close();
            }
        }

        private void BulkCopy(SqlConnection source, SqlConnection destination, TransferParameters transferParameters)
        {
            string sql = "";
            string table = transferParameters.Table;
            string query = "";
            if (CopyTables.dictionary[table] == "TABLE") query = transferParameters.TransferQuery;
            if (string.IsNullOrEmpty(query))
            {
                sql = $"select * from {table}";
            }
            else
            {
                CreateTempTable(source);
                InsertQueryData(source, transferParameters);
                sql = $"select * from {table} where UWI in (select UWI from #PDOList)";
            }

            using (SqlCommand cmd = new SqlCommand(sql, source))
            {
                try
                {
                    cmd.CommandTimeout = 3600;
                    SqlDataReader reader = cmd.ExecuteReader();
                    SqlBulkCopy bulkData = new SqlBulkCopy(destination);
                    bulkData.DestinationTableName = table;
                    bulkData.BulkCopyTimeout = 1000;
                    bulkData.WriteToServer(reader);
                    bulkData.Close();
                }
                catch (SqlException ex)
                {
                    Exception error = new Exception($"Sorry! Error copying table: {table}; {ex}");
                    throw error;
                }
            }
        }

        private void CreateTempTable(SqlConnection source)
        {
            string sql = "create table #PDOList (UWI NVARCHAR(40))";
            using (SqlCommand cmd = new SqlCommand(sql, source))
            {
                try
                {
                    cmd.CommandTimeout = 3000;
                    cmd.ExecuteNonQuery();
                }
                catch (SqlException ex)
                {
                    Exception error = new Exception("Error inserting into table: ", ex);
                    throw error;
                }

            }
        }

        private void InsertQueryData(SqlConnection source, TransferParameters transferParameters)
        {
            string query = transferParameters.TransferQuery;
            string queryType = transferParameters.QueryType;
            string sql;
            if (queryType == "File")
            {
                sql = $"DECLARE @Array NVARCHAR(MAX) = '{query}' " +
                    "INSERT INTO #PDOList ( UWI ) " +
                    "SELECT * FROM STRING_SPLIT(@Array, ',')";
            }
            else
            {
                sql = $"INSERT INTO #PDOList (UWI) SELECT UWI FROM WELL {query} ";
            }

            using (SqlCommand cmd = new SqlCommand(sql, source))
            {
                try
                {
                    cmd.CommandTimeout = 3000;
                    cmd.ExecuteNonQuery();
                }
                catch (SqlException ex)
                {
                    Exception error = new Exception("Error inserting into table: ", ex);
                    throw error;
                }

            }
        }
    }
}
