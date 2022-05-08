using DatabaseManager.Common.Entities;
using DatabaseManager.Common.Helpers;
using DatabaseManager.Common.Services;
using DatabaseManager.Shared;
using Microsoft.Azure.Cosmos.Table;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DatabaseManager.LocalDataTransfer
{
    public class DataTransfer
    {
        private readonly ILogger<Worker> _logger;
        private readonly AppSettings _appSeting;
        private readonly IQueueService _queueService;
        private DbUtilities _dbConn;
        private readonly IFileStorageServiceCommon _fileStorage;
        private ConnectParameters _targetConnector;
        private TransferParameters _transferParameters;
        private readonly string container = "sources";
        private readonly string infoQueue = "datatransferinfo";
        private string target;
        private string source;
        private string transferQuery;
        private string queryType;
        private string _referenceJson;

        private readonly Dictionary<string, string> dictionary = new Dictionary<string, string>
        {
            { "WELL", "TABLE" },
            { "BUSINESS_ASSOCIATE", "REFERENCE" },
            { "FIELD", "REFERENCE" },
            { "R_WELL_DATUM_TYPE", "REFERENCE" },
            { "R_WELL_STATUS", "REFERENCE" },
            { "STRAT_NAME_SET", "REFERENCE" },
            { "STRAT_UNIT", "REFERENCE"},
            { "STRAT_WELL_SECTION", "TABLE"},
            { "WELL_LOG_CURVE", "TABLE"},
            { "WELL_LOG_CURVE_VALUE", "TABLE"}
        };

        public DataTransfer(ILogger<Worker> logger,
            AppSettings appSetting,
            IQueueService queueService)
        {
            _dbConn = new DbUtilities();
            _logger = logger;
            _appSeting = appSetting;
            _queueService = queueService;
            var builder = new ConfigurationBuilder();
            IConfiguration configuration = builder.Build();
            _fileStorage = new AzureFileStorageServiceCommon(configuration);
            _fileStorage.SetConnectionString(appSetting.StorageAccount);
        }

        public async Task GetTransferConnector(string message)
        {
            try
            {
                TransferParameters transParms = JsonConvert.DeserializeObject<TransferParameters>(message);
                _targetConnector = GetConnectionString(transParms.TargetName);
                string dataAccessDefinition = await _fileStorage.ReadFile("connectdefinition", "PPDMDataAccess.json");
                _targetConnector.DataAccessDefinition = dataAccessDefinition;
                target = _targetConnector.ConnectionString;
                _logger.LogInformation($"Target connect string: {target}");
                ConnectParameters sourceConnector = GetConnectionString(transParms.SourceName);
                source = sourceConnector.ConnectionString;
                _logger.LogInformation($"Source connect string: {source}");
                transferQuery = transParms.TransferQuery;
                queryType = transParms.QueryType;
                
                _referenceJson = await _fileStorage.ReadFile("connectdefinition", "PPDMReferenceTables.json");
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
                string info = "";
                _dbConn.OpenWithConnectionString(target);
                foreach (string tableName in DatabaseTables.Names)
                {
                    info = $"Deleting table {tableName}";
                    _logger.LogInformation(info);
                    _queueService.InsertMessage(infoQueue, info);
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
                string info = "";
                sourceConn = new SqlConnection(source);
                destinationConn = new SqlConnection(target);
                sourceConn.Open();
                destinationConn.Open();

                CreateTempTable(sourceConn);
                InsertQueryData(sourceConn);

                foreach (string tableName in DatabaseTables.Names)
                {
                    info = $"Copying table {tableName}";
                    _queueService.InsertMessage(infoQueue, info);
                    _logger.LogInformation(info);
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

        private void ProcessReferenceTables(string table, SqlConnection source, SqlConnection destination)
        {
            string dataType = "";
            List<DataAccessDef> accessDefList = JsonConvert.DeserializeObject<List<DataAccessDef>>(_targetConnector.DataAccessDefinition);
            foreach (DataAccessDef accessDef in accessDefList)
            {
                string selectTable = Common.Helpers.Common.GetTable(accessDef.Select);
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
                foreach (var reference in references)
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
            DeleteTable(reference.Table);
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
                    string info = $"Creating table {reference.Table}";
                    _queueService.InsertMessage(infoQueue, info);
                    _logger.LogInformation(info);
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
            if (!string.IsNullOrEmpty(transferQuery))
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

        private void BulkCopy(SqlConnection source, SqlConnection destination, string table)
        {
            string sql = "";
            string query = "";
            if (dictionary[table] == "TABLE") query = transferQuery;
            if (string.IsNullOrEmpty(query))
            {
                sql = $"select * from {table}";
            }
            else
            {
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

        private void InsertQueryData(SqlConnection source)
        {
            string sql;
            if (queryType == "File")
            {
                sql = $"DECLARE @Array NVARCHAR(MAX) = '{transferQuery}' " +
                    "INSERT INTO #PDOList ( UWI ) " +
                    "SELECT * FROM STRING_SPLIT(@Array, ',')";
            }
            else
            {
                sql = $"INSERT INTO #PDOList (UWI) SELECT UWI FROM WELL {transferQuery} ";
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

        private void DeleteTable(string tableName)
        {
            try
            {
                string info = "";
                _dbConn.OpenWithConnectionString(target);
                info = $"Deleting table {tableName}";
                _logger.LogInformation(info);
                _queueService.InsertMessage(infoQueue, info);
                _dbConn.DBDelete(tableName);
            }
            catch (Exception ex)
            {
                _logger.LogInformation($"Error deleting table {ex.ToString()}");
            }
            finally
            {
                _dbConn.CloseConnection();
            }
        }

        private ConnectParameters GetConnectionString(string name)
        {
            ConnectParameters connectParms = new ConnectParameters();
            CloudTable table = GetTableConnect(_appSeting.StorageAccount, container);
            TableOperation retrieveOperation = TableOperation.Retrieve<SourceEntity>("PPDM", name);
            TableResult result = table.Execute(retrieveOperation);
            SourceEntity data = (SourceEntity)result.Result;
            connectParms.SourceName = name;
            connectParms.SourceType = data.SourceType;
            connectParms.Catalog = data.Catalog;
            connectParms.DatabaseServer = data.DatabaseServer;
            connectParms.Password = data.Password;
            connectParms.User = data.User;
            connectParms.ConnectionString = data.ConnectionString;
            connectParms.DataType = data.DataType;
            connectParms.FileName = data.FileName;
            connectParms.CommandTimeOut = data.CommandTimeOut;
            return connectParms;
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
