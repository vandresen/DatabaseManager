using DatabaseManager.Components;
using DatabaseManager.Shared;
using Microsoft.Azure.Cosmos.Table;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DatabaseManager.LocalDataTransfer
{
    public class DataTransfer
    {
        private readonly ILogger<Worker> _logger;
        private readonly AppSettings _appSeting;
        private DbUtilities _dbConn;
        private readonly string container = "sources";
        private string target;
        private string source;

        public DataTransfer(ILogger<Worker> logger,
            AppSettings appSeting)
        {
            _dbConn = new DbUtilities();
            _logger = logger;
            _appSeting = appSeting;
        }

        public void GetTransferConnector(string message)
        {
            try
            {
                TransferParameters transParms = JsonConvert.DeserializeObject<TransferParameters>(message);
                target = GetConnectionString(transParms.TargetName);
                _logger.LogInformation($"Target connect string: {target}");
                source = GetConnectionString(transParms.SourceName);
                _logger.LogInformation($"Source connect string: {source}");
            }
            catch (Exception ex)
            {
                _logger.LogInformation($"Error getting connector info {ex.ToString()}");
            }
            
        }

        public void DeleteTables()
        {
            try
            {
                _dbConn.OpenWithConnectionString(target);
                foreach (string tableName in DatabaseTables.Names)
                {
                    _logger.LogInformation($"Deleteing table {tableName}");
                    _dbConn.DBDelete(tableName);
                }
            }
            catch (Exception ex)
            {
                _logger.LogInformation($"Error deleting tables {ex.ToString()}");
            }
            finally
            {
                _dbConn.CloseConnection();
            }
            
        }

        public void CopyTables()
        {
            SqlConnection sourceConn = new SqlConnection();
            SqlConnection destinationConn = new SqlConnection();
            try
            {
                sourceConn = new SqlConnection(source);
                destinationConn = new SqlConnection(target);
                sourceConn.Open();
                destinationConn.Open();

                foreach (string tableName in DatabaseTables.Names)
                {
                    _logger.LogInformation($"Copying table {tableName}");
                    BulkCopy(sourceConn, destinationConn, tableName);
                }
            }
            catch (Exception ex)
            {
                _logger.LogInformation($"Error copying tables {ex.ToString()}");
            }
            finally
            {
                sourceConn.Close();
                destinationConn.Close();
            }

        }

        private void BulkCopy(SqlConnection source, SqlConnection destination, string table)
        {
            string sql = $"select * from {table}";
            
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

        private string GetConnectionString(string name)
        {
            string cnStr = "";

            CloudTable table = GetTableConnect(_appSeting.StorageAccount, container);
            TableOperation retrieveOperation = TableOperation.Retrieve<SourceEntity>("PPDM", name);
            TableResult result = table.Execute(retrieveOperation);
            SourceEntity data = (SourceEntity)result.Result;
            cnStr = data.ConnectionString;

            return cnStr;
        }

        private CloudTable GetTableConnect(string connectionString, string tableName)
        {
            CloudStorageAccount account = CloudStorageAccount.Parse(connectionString);
            CloudTableClient client = account.CreateCloudTableClient();
            CloudTable table = client.GetTableReference(tableName);
            return table;
        }
    }
}
