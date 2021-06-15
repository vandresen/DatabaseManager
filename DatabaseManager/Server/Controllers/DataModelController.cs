using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using DatabaseManager.Server.Helpers;
using DatabaseManager.Shared;
using Microsoft.AspNetCore.Hosting;
using DatabaseManager.Server.Entities;
using System.Data;
using Newtonsoft.Json;
using DatabaseManager.Server.Services;
using Microsoft.Extensions.Logging;
using AutoMapper;

namespace DatabaseManager.Server.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class DataModelController : ControllerBase
    {
        private readonly IFileStorageService fileStorageService;
        private readonly ITableStorageService tableStorageService;
        private readonly IMapper mapper;
        private readonly ILogger<DataModelController> logger;
        private readonly IWebHostEnvironment _env;
        private string connectionString;
        private string _credentials;
        private string _secret;
        private readonly string _contentRootPath;
        private readonly string container = "sources";

        public DataModelController(IConfiguration configuration,
            IFileStorageService fileStorageService,
            ITableStorageService tableStorageService,
            IMapper mapper,
            ILogger<DataModelController> logger,
            IWebHostEnvironment env)
        {
            connectionString = configuration.GetConnectionString("AzureStorageConnection");
            _credentials = configuration["BlobCredential"];
            _secret = configuration["BlobSecret"];
            this.fileStorageService = fileStorageService;
            this.tableStorageService = tableStorageService;
            this.mapper = mapper;
            this.logger = logger;
            _env = env;
            _contentRootPath = _env.ContentRootPath;
        }

        [HttpPost]
        public async Task<ActionResult<string>> DataModelCreate(DataModelParameters dmParameters)
        {
            logger.LogInformation("Starting data model create");
            if (dmParameters == null) return BadRequest();
            try
            {
                string tmpConnString = Request.Headers["AzureStorageConnection"];
                fileStorageService.SetConnectionString(tmpConnString);
                tableStorageService.SetConnectionString(tmpConnString);
                //SourceEntity connector = new SourceEntity();
                SourceEntity entity = await tableStorageService.GetTableRecord<SourceEntity>(container, dmParameters.DataConnector);
                ConnectParameters connector = mapper.Map<ConnectParameters>(entity);
                if (connector == null) return BadRequest();
                if (dmParameters.ModelOption == "PPDM Model")
                {
                    await CreatePPDMModel(dmParameters, connector);
                }
                else if (dmParameters.ModelOption == "DSM Model")
                {
                    await CreateDMSModel(dmParameters, connector);
                }
                else if (dmParameters.ModelOption == "DSM Rules")
                {
                    await CreateDSMRules(connector);
                }
                else if (dmParameters.ModelOption == "Stored Procedures")
                {
                    await CreateStoredProcedures(connector);
                    CreateFunctions(connector);
                }
                else if (dmParameters.ModelOption == "PPDM Modifications")
                {
                    CreatePpdmModifications(connector);
                }
                else
                {

                }
            }
            catch (Exception ex)
            {
                return BadRequest(ex.ToString());
            }

            return Ok($"OK");
        }

        private async Task CreateStoredProcedures(ConnectParameters connector)
        {
            try
            {
                string contentRootPath = _env.ContentRootPath;
                string sqlFile = contentRootPath + @"\DataBase\StoredProcedures.sql";
                string sql = System.IO.File.ReadAllText(sqlFile);
                string[] commandText = sql.Split(new string[] { String.Format("{0}GO{0}", Environment.NewLine) }, StringSplitOptions.RemoveEmptyEntries);
                DbUtilities dbConn = new DbUtilities();
                dbConn.OpenConnection(connector);
                for (int x = 0; x < commandText.Length; x++)
                {
                    if (commandText[x].Trim().Length > 0)
                    {
                        dbConn.SQLExecute(commandText[x]);
                    }
                }

                await CreateInsertStoredProcedure(dbConn);
                await CreateUpdateStoredProcedure(dbConn);
                
                dbConn.CloseConnection();
            }
            catch (Exception ex)
            {
                Exception error = new Exception("Create DMS Model Error: ", ex);
                throw error;
            }
        }

        private void CreateFunctions(ConnectParameters connector)
        {
            string contentRootPath = _env.ContentRootPath;
            string sqlFile = contentRootPath + @"\DataBase\Functions.sql";
            string sql = System.IO.File.ReadAllText(sqlFile);
            string[] commandText = sql.Split(new string[] { String.Format("{0}GO{0}", Environment.NewLine) }, StringSplitOptions.RemoveEmptyEntries);
            DbUtilities dbConn = new DbUtilities();
            dbConn.OpenConnection(connector);
            for (int x = 0; x < commandText.Length; x++)
            {
                if (commandText[x].Trim().Length > 0)
                {
                    dbConn.SQLExecute(commandText[x]);
                }
            }
        }

        private async Task CreateInsertStoredProcedure(DbUtilities dbConn)
        {
            string comma;
            string attributes;

            List<DataAccessDef> accessDefs = await GetDataAccessDefinitions();
            DbDataTypes dbDataTypes = new DbDataTypes();
            for (int j = 0; j < dbDataTypes.DataTypes.Length; j++)
            {
                string dataType = dbDataTypes.DataTypes[j];
                string sqlCommand = $"DROP PROCEDURE IF EXISTS spInsert{dataType} ";
                dbConn.SQLExecute(sqlCommand);
                sqlCommand = "";

                DataAccessDef accessDef = accessDefs.First(x => x.DataType == dataType);
                string sql = accessDef.Select;
                string[] keys = accessDef.Keys.Split(',');

                string table = GetTable(sql);
                ColumnProperties attributeProperties = GetColumnSchema(dbConn, sql);
                string[] tableAttributes = Common.GetAttributes(sql);
                tableAttributes = tableAttributes.Where(w => w != "Id").ToArray();

                sqlCommand = sqlCommand + $"CREATE PROCEDURE spInsert{dataType} ";
                sqlCommand = sqlCommand + " @json NVARCHAR(max) ";
                sqlCommand = sqlCommand + " AS ";
                sqlCommand = sqlCommand + " BEGIN ";

                sqlCommand = sqlCommand + $" INSERT INTO {table }";
                attributes = " (";
                comma = "";
                foreach (var word in tableAttributes)
                {
                    string attribute = word.Trim();
                    attributes = attributes + comma + "[" + attribute + "]";
                    comma = ",";
                }
                attributes = attributes + ")";
                sqlCommand = sqlCommand + attributes;

                sqlCommand = sqlCommand + $"  SELECT";
                comma = "    ";
                attributes = "";
                foreach (var word in tableAttributes)
                {
                    string attribute = word.Trim();
                    attributes = attributes + comma + attribute;
                    comma = ",";
                }
                sqlCommand = sqlCommand + attributes;
                sqlCommand = sqlCommand + $" FROM OPENJSON(@json)";

                comma = "";
                attributes = "    WITH (";
                foreach (var word in tableAttributes)
                {
                    string attribute = word.Trim();
                    string dataProperty = attributeProperties[attribute];
                    attributes = attributes + comma + attribute + " " + dataProperty +
                        " '$." + attribute + "'";
                    comma = ",";
                }
                sqlCommand = sqlCommand + attributes;
                sqlCommand = sqlCommand + ") AS jsonValues ";

                sqlCommand = sqlCommand + " END";
                dbConn.SQLExecute(sqlCommand);
            }
        }

        private async Task CreateUpdateStoredProcedure(DbUtilities dbConn)
        {
            string comma;
            string attributes;

            List<DataAccessDef> accessDefs = await GetDataAccessDefinitions();
            DbDataTypes dbDataTypes = new DbDataTypes();
            for (int j = 0; j < dbDataTypes.DataTypes.Length; j++)
            {
                string dataType = dbDataTypes.DataTypes[j];
                string sqlCommand = $"DROP PROCEDURE IF EXISTS spUpdate{dataType} ";
                dbConn.SQLExecute(sqlCommand);
                sqlCommand = "";

                DataAccessDef accessDef = accessDefs.First(x => x.DataType == dataType);
                string sql = accessDef.Select;
                string[] keys = accessDef.Keys.Split(',');

                string table = GetTable(sql);
                ColumnProperties attributeProperties = GetColumnSchema(dbConn, sql);
                string[] tableAttributes = Common.GetAttributes(sql);

                sqlCommand = sqlCommand + $"CREATE PROCEDURE spUpdate{dataType} ";
                sqlCommand = sqlCommand + "@json NVARCHAR(max) ";
                sqlCommand = sqlCommand + "AS ";
                sqlCommand = sqlCommand + "BEGIN ";

                sqlCommand = sqlCommand + $"SELECT ";
                comma = "    ";
                attributes = "";
                foreach (var word in tableAttributes)
                {
                    string attribute = word.Trim();
                    attributes = attributes + comma + attribute;
                    comma = ",";
                }
                sqlCommand = sqlCommand + attributes;

                sqlCommand = sqlCommand + " INTO #TempJson ";
                sqlCommand = sqlCommand + $" FROM OPENJSON(@json) ";
                comma = "";
                attributes = "    WITH (";
                foreach (var word in tableAttributes)
                {
                    string attribute = word.Trim();
                    string dataProperty = attributeProperties[attribute];
                    attributes = attributes + comma + attribute + " " + dataProperty +
                        " '$." + attribute + "'";
                    comma = ",";
                }
                sqlCommand = sqlCommand + attributes;
                sqlCommand = sqlCommand + ") AS jsonValues ";

                sqlCommand = sqlCommand + $" UPDATE A ";
                sqlCommand = sqlCommand + $" SET ";
                comma = "    ";
                attributes = "";
                foreach (var word in tableAttributes)
                {
                    if (word != "Id")
                    {
                        string attribute = word.Trim();
                        attributes = attributes + comma + "A." + attribute + " = " + "B." + attribute;
                        comma = ",";
                    }
                }
                sqlCommand = sqlCommand + attributes;
                sqlCommand = sqlCommand + $" FROM ";
                sqlCommand = sqlCommand + $" {table} AS A ";
                sqlCommand = sqlCommand + " INNER JOIN #TempJson AS B ON ";
                comma = "    ";
                attributes = "";
                foreach (string key in keys)
                {
                    attributes = attributes + comma + "A." + key.Trim() + " = " + "B." + key.Trim();
                    comma = " AND ";
                }
                sqlCommand = sqlCommand + attributes;
                sqlCommand = sqlCommand + " END";

                dbConn.SQLExecute(sqlCommand);
            }
        }

        static ColumnProperties GetColumnSchema(DbUtilities dbConn, string sql)
        {
            ColumnProperties colProps = new ColumnProperties();
            string attributeType = "";
            string table = GetTable(sql);
            string select = $"Select * from INFORMATION_SCHEMA.COLUMNS ";
            string query = $" where TABLE_NAME = '{table}'";
            DataTable dt = dbConn.GetDataTable(select, query);

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
                    Console.WriteLine("Warning: attribute not found");
                }

                colProps[attributeIndex] = attributeType;
            }

            return colProps;
        }

        static string GetTable(string select)
        {
            select = select.ToUpper();
            int from = select.IndexOf(" FROM ") + 6;
            string table = select.Substring(from);
            return table;
        }

        private async Task CreateDMSModel(DataModelParameters dmParameters, ConnectParameters connector)
        {
            try
            {
                string sqlFile = _contentRootPath + @"\DataBase\DataScienceManagement.sql";
                string sql = System.IO.File.ReadAllText(sqlFile);
                sqlFile = _contentRootPath + @"\DataBase\InternalRuleFunctions.sql";
                string sqlFunctions = System.IO.File.ReadAllText(sqlFile);
                DbUtilities dbConn = new DbUtilities();
                dbConn.OpenConnection(connector);
                dbConn.SQLExecute(sql);
                dbConn.SQLExecute(sqlFunctions);
                CreateSqlSources(dbConn);
                dbConn.CloseConnection();

                string fileName = "WellBore.json";
                string taxonomyFile = _contentRootPath + @"\DataBase\WellBore.json";
                string taxonomy = System.IO.File.ReadAllText(taxonomyFile);
                await fileStorageService.SaveFile(dmParameters.FileShare, fileName, taxonomy);

                fileName = "PPDMDataAccess.json";
                string defFile = _contentRootPath + @"\DataBase\PPDMDataAccess.json";
                string definition = System.IO.File.ReadAllText(defFile);
                await fileStorageService.SaveFile("connectdefinition", fileName, definition);

                fileName = "CSVDataAccess.json";
                defFile = _contentRootPath + @"\DataBase\CSVDataAccess.json";
                definition = System.IO.File.ReadAllText(defFile);
                await fileStorageService.SaveFile("connectdefinition", fileName, definition);

                fileName = "PPDMReferenceTables.json";
                defFile = _contentRootPath + @"\DataBase\PPDMReferenceTables.json";
                definition = System.IO.File.ReadAllText(defFile);
                await fileStorageService.SaveFile("connectdefinition", fileName, definition);
            }
            catch (Exception ex)
            {
                Exception error = new Exception("Create DMS Model Error: ", ex);
                throw error;
            }
        }

        private void CreateSqlSources(DbUtilities dbConn)
        {
            string sql = "";
            //string credentials = "PDOAzureBlobsCredentials";
            //string secret = @"sv=2019-12-12&st=2021-06-14T19%3A23%3A15Z&se=2021-06-15T19%3A23%3A15Z&sr=c&sp=rl&sig=Lnif244ps%2BlBWWUb2fyBtTPu69gnESrMnzSkre8V3%2BA%3D";
            try
            {
                sql = @"CREATE MASTER KEY ENCRYPTION BY PASSWORD = 'MasterKeyAzureBlobs'";
                dbConn.SQLExecute(sql);
            }
            catch (Exception ex)
            {
                logger.LogInformation("Problems creating master key, it may already exist, {ex}");
            }

            sql = "Select * from sys.external_data_sources ";
            string query = " where name = 'PDOAzureBlob'";
            DataTable dt = dbConn.GetDataTable(sql, query);
            if (dt.Rows.Count > 0)
            {
                sql = "DROP EXTERNAL DATA SOURCE PDOAzureBlob ";
                dbConn.SQLExecute(sql);
            }

            try
            {
                sql = $"DROP DATABASE SCOPED CREDENTIAL {_credentials}";
                dbConn.SQLExecute(sql);
            }
            catch (Exception ex)
            {
                logger.LogInformation("Problems deleting credentials, it may not exist, {ex}");
            }
            sql = $"CREATE DATABASE SCOPED CREDENTIAL {_credentials} WITH IDENTITY = 'SHARED ACCESS SIGNATURE', SECRET = '{_secret}'";
            dbConn.SQLExecute(sql);
            
            string blobStorage = @"https://petrodataonlinestorage.blob.core.windows.net/welldata";
            sql = $"CREATE EXTERNAL DATA SOURCE PDOAzureBlob WITH(TYPE = BLOB_STORAGE, LOCATION = '{blobStorage}', CREDENTIAL = {_credentials})";
            dbConn.SQLExecute(sql);
        }

        private async Task CreateDSMRules(ConnectParameters connector)
        {
            try
            {
                List<DataAccessDef> accessDefs = await GetDataAccessDefinitions();
                DbUtilities dbConn = new DbUtilities();
                dbConn.OpenConnection(connector);
                RuleUtilities.SaveRulesFile(dbConn, _contentRootPath, accessDefs);
                dbConn.CloseConnection();
            }
            catch (Exception ex)
            {
                Exception error = new Exception("Load DMS Rule Error: ", ex);
                throw error;
            }
        }

        private void CreatePpdmModifications(ConnectParameters connector)
        {
            try
            {
                string contentRootPath = _env.ContentRootPath;
                string sqlFile = contentRootPath + @"\DataBase\PpdmModifications.sql";
                string sql = System.IO.File.ReadAllText(sqlFile);
                string[] commandText = sql.Split(new string[] { String.Format("{0}GO{0}", Environment.NewLine) }, StringSplitOptions.RemoveEmptyEntries);
                DbUtilities dbConn = new DbUtilities();
                dbConn.OpenConnection(connector);
                for (int x = 0; x < commandText.Length; x++)
                {
                    if (commandText[x].Trim().Length > 0)
                    {
                        dbConn.SQLExecute(commandText[x]);
                    }
                }
                dbConn.CloseConnection();
            }
            catch (Exception ex)
            {
                Exception error = new Exception("Create DMS Model Error: ", ex);
                throw error;
            }
        }

        

        private async Task CreatePPDMModel(DataModelParameters dmParameters, ConnectParameters connector)
        {
            try
            {
                string sql = await fileStorageService.ReadFile(dmParameters.FileShare, dmParameters.FileName);
                if (string.IsNullOrEmpty(sql))
                {
                    Exception error = new Exception($"Empty data from {dmParameters.FileName}");
                    throw error;
                }
                else
                {
                    DbUtilities dbConn = new DbUtilities();
                    dbConn.OpenConnection(connector);
                    dbConn.SQLExecute(sql);
                    dbConn.CloseConnection();
                }
            }
            catch (Exception ex)
            {
                Exception error = new Exception("Create PPDM Model Error: ", ex);
                throw error;
            }
        }

        private async Task<List<DataAccessDef>> GetDataAccessDefinitions()
        {
            string accessJson = await fileStorageService.ReadFile("connectdefinition", "PPDMDataAccess.json");
            List<DataAccessDef> accessDefs = JsonConvert.DeserializeObject<List<DataAccessDef>>(accessJson);
            return accessDefs;
        }
    }
}