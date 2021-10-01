﻿using AutoMapper;
using DatabaseManager.Common.Entities;
using DatabaseManager.Common.Services;
using DatabaseManager.Shared;
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DatabaseManager.Common.Helpers
{
    public class DataModelManagement
    {
        private readonly string fileShare = "database";
        private readonly string azureConnectionString;
        private readonly string _sqlRootPath;
        private readonly IFileStorageServiceCommon _fileStorage;
        private readonly ITableStorageServiceCommon _tableStorage;
        private DbUtilities _dbConn;
        private IMapper _mapper;

        public DataModelManagement(string azureConnectionString, string sqlRootPath)
        {
            _sqlRootPath = sqlRootPath;
            this.azureConnectionString = azureConnectionString;
            var builder = new ConfigurationBuilder();
            IConfiguration configuration = builder.Build();
            _fileStorage = new AzureFileStorageServiceCommon(configuration);
            _fileStorage.SetConnectionString(azureConnectionString);
            _tableStorage = new AzureTableStorageServiceCommon(configuration);
            _tableStorage.SetConnectionString(azureConnectionString);
            _dbConn = new DbUtilities();

            var config = new MapperConfiguration(cfg => {
                cfg.CreateMap<SourceEntity, ConnectParameters>().ForMember(dest => dest.SourceName, opt => opt.MapFrom(src => src.RowKey));
                cfg.CreateMap<ConnectParameters, SourceEntity>().ForMember(dest => dest.RowKey, opt => opt.MapFrom(src => src.SourceName));
            });
            _mapper = config.CreateMapper();
        }

        public async Task DataModelCreate(DataModelParameters dmParameters)
        {
            ConnectParameters connector = await Common.GetConnectParameters(azureConnectionString, dmParameters.DataConnector);
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
                await CreateFunctions(connector);
            }
            else if (dmParameters.ModelOption == "PPDM Modifications")
            {
                await CreatePpdmModifications(connector);
            }
            else
            {

            }
        }

        private async Task CreateDSMRules(ConnectParameters connector)
        {
            try
            {
                List<DataAccessDef> accessDefs = await GetDataAccessDefinitions();
                _dbConn.OpenConnection(connector);
                string ruleString = await ReadDatabaseFile("StandardRules.json");
                SaveRulesToDatabase(ruleString, accessDefs);
                _dbConn.CloseConnection();
            }
            catch (Exception ex)
            {
                Exception error = new Exception("Load DMS Rule Error: ", ex);
                throw error;
            }
        }

        private void SaveRulesToDatabase(string jsonRule, List<DataAccessDef> accessDefs)
        {
            try
            {
                List<RuleFunctions> ruleFunctions = new List<RuleFunctions>();
                DataAccessDef accessDef = accessDefs.First(x => x.DataType == "Functions");
                string sql = accessDef.Select;
                string query = "";
                DataTable ft = _dbConn.GetDataTable(sql, query);

                JArray JsonRuleArray = JArray.Parse(jsonRule);
                foreach (JToken rule in JsonRuleArray)
                {
                    string function = rule["RuleFunction"].ToString();
                    string functionType = "";
                    if (rule["RuleType"].ToString() == "Validity") functionType = "V";
                    if (rule["RuleType"].ToString() == "Predictions") functionType = "P";
                    RuleFunctions ruleFunction = SaveRuleFunction(function, functionType);
                    string functionName = ruleFunction.FunctionName;
                    string functionQuery = $"FunctionName = '{functionName}'";
                    DataRow[] rows = ft.Select(functionQuery);
                    if (rows.Length == 0)
                    {
                        RuleFunctions result = ruleFunctions.FirstOrDefault(s => s.FunctionName == functionName);
                        if (result == null) ruleFunctions.Add(ruleFunction);
                    }

                    rule["RuleFunction"] = ruleFunction.FunctionName;
                    string jsonDate = DateTime.Now.ToString("yyyy-MM-dd");
                    rule["CreatedDate"] = jsonDate;
                    rule["ModifiedDate"] = jsonDate;
                }
                string jsonFunctions = JsonConvert.SerializeObject(ruleFunctions, Formatting.Indented);
                _dbConn.InsertDataObject(jsonFunctions, "Functions");

                string json = JsonRuleArray.ToString();
                _dbConn.InsertDataObject(json, "Rules");
            }
            catch (Exception ex)
            {
                Exception error = new Exception($"Error saving rule: {ex}");
                throw;
            }
        }

        private RuleFunctions SaveRuleFunction(string ruleFunction, string functionType)
        {
            RuleFunctions rf = new RuleFunctions();

            int startFunctionName = ruleFunction.IndexOf(@"/api/");
            if (startFunctionName == -1) startFunctionName = 0;
            else startFunctionName = startFunctionName + 5;

            int endFunctionName = ruleFunction.IndexOf(@"?");
            string functionKey = "";
            if (endFunctionName == -1)
            {
                endFunctionName = ruleFunction.Length;
            }
            else
            {
                functionKey = ruleFunction.Substring((endFunctionName + 6));
            }

            int functionNameLength = endFunctionName - startFunctionName;
            string functionname = ruleFunction.Substring(startFunctionName, functionNameLength);
            string functionUrl = ruleFunction.Substring(0, endFunctionName);

            rf.FunctionName = functionname;
            rf.FunctionUrl = functionUrl;
            rf.FunctionKey = functionKey;
            rf.FunctionType = functionType;
            return rf;
        }

        private async Task CreateStoredProcedures(ConnectParameters connector)
        {
            try
            {
                string sql = await ReadDatabaseFile("StoredProcedures.sql");
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

        private async Task CreateFunctions(ConnectParameters connector)
        {
            string sql = await ReadDatabaseFile("Functions.sql");
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

        private async Task CreateDMSModel(DataModelParameters dmParameters, ConnectParameters connector)
        {
            try
            {
                string sql = await ReadDatabaseFile("DataScienceManagement.sql");
                string sqlFunctions = await ReadDatabaseFile("InternalRuleFunctions.sql");
                DbUtilities dbConn = new DbUtilities();
                dbConn.OpenConnection(connector);
                dbConn.SQLExecute(sql);
                dbConn.SQLExecute(sqlFunctions);
                //CreateSqlSources(dbConn);
                dbConn.CloseConnection();

                string fileName = "WellBore.json";
                string taxonomy = await ReadDatabaseFile(fileName);
                await _fileStorage.SaveFile(dmParameters.FileShare, fileName, taxonomy);

                fileName = "PPDMDataAccess.json";
                string definition = await ReadDatabaseFile(fileName);
                await _fileStorage.SaveFile("connectdefinition", fileName, definition);

                fileName = "LASDataAccess.json";
                definition = await ReadDatabaseFile(fileName);
                await _fileStorage.SaveFile("connectdefinition", fileName, definition);

                fileName = "CSVDataAccess.json";
                definition = await ReadDatabaseFile(fileName);
                await _fileStorage.SaveFile("connectdefinition", fileName, definition);

                fileName = "PPDMReferenceTables.json";
                definition = await ReadDatabaseFile(fileName);
                await _fileStorage.SaveFile("connectdefinition", fileName, definition);
            }
            catch (Exception ex)
            {
                Exception error = new Exception("Create DMS Model Error: ", ex);
                throw error;
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

                string table = Common.GetTable(sql);
                ColumnProperties attributeProperties = CommonDbUtilities.GetColumnSchema(dbConn, sql);
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

                string table = Common.GetTable(sql);
                ColumnProperties attributeProperties = CommonDbUtilities.GetColumnSchema(dbConn, sql);
                string[] tableAttributes = Helpers.Common.GetAttributes(sql);

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

        private async Task CreatePpdmModifications(ConnectParameters connector)
        {
            try
            {
                string sql = await ReadDatabaseFile("PpdmModifications.sql");
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
                string sql = await _fileStorage.ReadFile(dmParameters.FileShare, dmParameters.FileName);
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

        private void CreateSqlSources(DbUtilities dbConn)
        {
            string sql = "";
            try
            {
                sql = @"CREATE MASTER KEY ENCRYPTION BY PASSWORD = 'MasterKeyAzureBlobs'";
                dbConn.SQLExecute(sql);
            }
            catch (Exception ex)
            {
                //logger.LogInformation("Problems creating master key, it may already exist, {ex}");
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
                //sql = $"DROP DATABASE SCOPED CREDENTIAL {_credentials}";
                //dbConn.SQLExecute(sql);
            }
            catch (Exception ex)
            {
                //logger.LogInformation($"Problems deleting credentials, it may not exist, {ex}");
            }

            try
            {
                //sql = $"CREATE DATABASE SCOPED CREDENTIAL {_credentials} WITH IDENTITY = 'SHARED ACCESS SIGNATURE', SECRET = '{_secret}'";
                //dbConn.SQLExecute(sql);
                //sql = $"CREATE EXTERNAL DATA SOURCE PDOAzureBlob WITH(TYPE = BLOB_STORAGE, LOCATION = '{_blobStorage}', CREDENTIAL = {_credentials})";
                //dbConn.SQLExecute(sql);
            }
            catch (Exception ex)
            {
                //logger.LogInformation($"Problems crreating external data source, {ex}");
            }
        }

        private async Task<string> ReadDatabaseFile(string fileName)
        {
            string content = "";
            if (_sqlRootPath == null)
            {
                content = await _fileStorage.ReadFile(fileShare, fileName);
            }
            else
            {
                string sqlFile = _sqlRootPath + @"\DataBase\" + fileName;
                content = System.IO.File.ReadAllText(sqlFile);
            }

            return content;
        }

        private async Task<List<DataAccessDef>> GetDataAccessDefinitions()
        {
            string accessJson = await _fileStorage.ReadFile("connectdefinition", "PPDMDataAccess.json");
            List<DataAccessDef> accessDefs = JsonConvert.DeserializeObject<List<DataAccessDef>>(accessJson);
            return accessDefs;
        }
    }
}