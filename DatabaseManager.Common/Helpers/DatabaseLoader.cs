using DatabaseManager.Common.DBAccess;
using DatabaseManager.Common.Entities;
using DatabaseManager.Shared;
using Microsoft.Data.SqlClient;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Data;
using System.Text;

namespace DatabaseManager.Common.Helpers
{
    public class DatabaseLoader
    {

        private ConnectParameters _targetConnector;
        private TransferParameters _transferParameters;
        private string _sourceConnectString;
        private string _referenceJson;

        public DatabaseLoader()
        {

        }

        public void CopyTable(TransferParameters transferParameters, string sourceCnStr, ConnectParameters targetConnector, string referenceJson)
        {
            _targetConnector = targetConnector;
            _sourceConnectString = sourceCnStr;
            _referenceJson = referenceJson;
            _transferParameters = transferParameters;
            string destCnStr = targetConnector.ConnectionString;
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

        public void DeleteTable(string connectString, string table)
        {
            IADODataAccess db = new ADODataAccess();
            db.Delete(table, connectString);
        }

        private void ProcessReferenceTables(string table, SqlConnection source, SqlConnection destination)
        {
            string dataType = "";
            List<DataAccessDef> accessDefList = JsonConvert.DeserializeObject<List<DataAccessDef>>(_targetConnector.DataAccessDefinition);
            foreach (DataAccessDef accessDef in accessDefList)
            {
                string selectTable = Common.GetTable(accessDef.Select);
                if (selectTable == table)
                {
                    dataType = accessDef.DataType;
                    break;
                }
            }

            if (!string.IsNullOrEmpty(dataType))
            {
                List<ReferenceTable> allReferences = JsonConvert.DeserializeObject<List<ReferenceTable>>(_referenceJson);
                List<ReferenceTable> references = allReferences.FindAll(x => x.DataType == dataType);
                foreach(var reference in references)
                {
                    if (!DatabaseTables.Names.Contains(reference.Table))
                    {
                        BuildRefTable(reference, table, source, destination);
                    }
                }
            }
        }

        private void BuildRefTable(ReferenceTable reference, string table, SqlConnection source, SqlConnection destination)
        {
            DeleteTable(_targetConnector.ConnectionString, reference.Table);
            CreateRefTempTable(source, table, reference.ReferenceAttribute);
            string sqlCommand = $" SELECT A.* FROM {reference.Table} A INNER JOIN #REFID B ON A.{reference.KeyAttribute} = B.{reference.ReferenceAttribute}";
            using (SqlCommand cmd = new SqlCommand(sqlCommand, source))
            {
                try
                {
                    cmd.CommandTimeout = 3600;
                    SqlDataReader reader = cmd.ExecuteReader();
                    SqlBulkCopy bulkData = new SqlBulkCopy(destination);
                    bulkData.DestinationTableName = reference.Table;
                    bulkData.BulkCopyTimeout = 1000;
                    bulkData.WriteToServer(reader);
                }
                catch (SqlException ex)
                {
                    Exception error = new Exception($"Sorry! Error copying table: {table}; {ex}");
                    throw error;
                }
            }
        }

        private void CreateRefTempTable(SqlConnection source, string table, string referenceAttribute)
        {
            string sqlCommand = "DROP TABLE IF EXISTS #REFID";
            sqlCommand = sqlCommand + $" SELECT DISTINCT({referenceAttribute}) into #REFID from {table}";
            if (!string.IsNullOrEmpty(_transferParameters.TransferQuery))
            {
                sqlCommand = sqlCommand + $" where UWI in (select UWI from #PDOList)";
            }
            using (SqlCommand cmd = new SqlCommand(sqlCommand, source))
            {
                try
                {
                    cmd.CommandTimeout = 3000;
                    cmd.ExecuteNonQuery();
                }
                catch (SqlException ex)
                {
                    Exception error = new Exception("Error inserting into ref temp table: ", ex);
                    throw error;
                }

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

            ProcessReferenceTables(table, source, destination);

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
