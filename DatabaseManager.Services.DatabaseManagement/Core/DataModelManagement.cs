using DatabaseManager.Services.DatabaseManagement.Extensions;
using DatabaseManager.Services.DatabaseManagement.Initializer;
using DatabaseManager.Services.DatabaseManagement.Models;
using DatabaseManager.Services.DatabaseManagement.Services;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Data;
using System.Diagnostics.Eventing.Reader;
using System.Linq;
using System.Reflection;
using System.Runtime.Intrinsics.Arm;
using System.Text;
using System.Threading.Tasks;

namespace DatabaseManager.Services.DatabaseManagement.Core
{
    public class DataModelManagement
    {
        private readonly ILogger _logger;
        private readonly string _azureConnectionString;
        private string _dbConnectionString;
        private readonly ConnectParameters _connector;
        private readonly IFileStorageService _azureStorage;
        private readonly IFileStorageService _embeddedStorage;
        private readonly IDatabaseAccessService _DbStorage;
        private readonly IDbInitializer _dbi;

        public DataModelManagement(ILogger logger, string azureConnectionString, 
            ConnectParameters connector)
        {
            _logger = logger;
            _azureConnectionString = azureConnectionString;
            _connector = connector;
            _azureStorage = new AzureFileStorageService(azureConnectionString);
            _embeddedStorage = new EmbeddedFileStorageService();
            _DbStorage = new SQLServerAccessService();
            _dbi = new DbInitializer(_DbStorage);
        }

        public async Task DataModelCreate(DataModelParameters dmParameters)
        {
            _logger.LogInformation(dmParameters.ModelOption);
            if (dmParameters.ModelOption == "PPDM Model")
            {
                await CreatePPDMModel(dmParameters);
            }
            else if (dmParameters.ModelOption == "DSM Model")
            {
                await CreateDMSModel(dmParameters);
            }
            else if (dmParameters.ModelOption == "DSM Rules")
            {
                await CreateDSMRules();
            }
            else if (dmParameters.ModelOption == "Stored Procedures")
            {
                await CreateStoredProcedures(dmParameters);
                await CreateFunctions();
            }
            else if (dmParameters.ModelOption == "PPDM Modifications")
            {
                await CreatePpdmModifications(dmParameters);
            }
            else
            {
                _logger.LogInformation($"{dmParameters.ModelOption} is not available");
            } 
        }

        private async Task CreatePPDMModel(DataModelParameters dmParameters)
        {
            try
            {
                _logger.LogInformation($"Processing file {dmParameters.FileName}");
                string sql = await _azureStorage.ReadFile(dmParameters.FileShare, dmParameters.FileName);
                if (string.IsNullOrEmpty(sql))
                {
                    Exception error = new Exception($"Empty data from {dmParameters.FileName}");
                    throw error;
                }
                else
                {
                    string connectionString = CreateDatabaseConnectionString();
                    _DbStorage.ExecuteSQL(sql, connectionString);
                }
            }
            catch (Exception ex)
            {
                Exception error = new Exception("Create PPDM Model Error: ", ex);
                throw error;
            }
        }

        private async Task CreatePpdmModifications(DataModelParameters dmParameters)
        {
            try
            {
                _logger.LogInformation($"Processing file {dmParameters.FileName}");
                _dbConnectionString = CreateDatabaseConnectionString();
                string fileName = "PpdmModifications.sql";
                string sql = await _embeddedStorage.ReadFile(dmParameters.FileShare, fileName);
                string[] commandText = sql.Split(new string[] { String.Format("{0}GO{0}", Environment.NewLine) }, StringSplitOptions.RemoveEmptyEntries);
                await PopulateFixedKeys();
                
                for (int x = 0; x < commandText.Length; x++)
                {
                    if (commandText[x].Trim().Length > 0)
                    {
                        _DbStorage.ExecuteSQL(commandText[x], _dbConnectionString);
                    }
                }
            }
            catch (Exception ex)
            {
                Exception error = new Exception("Create DMS Model Error: ", ex);
                throw error;
            }
        }

        private async Task CreateDMSModel(DataModelParameters dmParameters)
        {
            try
            {
                _logger.LogInformation($"Creating DMS data model in database");
                string connectionString = CreateDatabaseConnectionString();
                _dbi.CreateDatabaseManagementTables(connectionString);
                _dbi.InitializeInternalRuleFunctions(connectionString);

                string fileName = "WellBore.json";
                string fileShare = "taxonomy";
                string taxonomy = await _embeddedStorage.ReadFile("", fileName);
                await _azureStorage.SaveFile(fileShare, fileName, taxonomy);
                _logger.LogInformation($"Taxonomy file copied to Azure storage");

                fileName = "PPDMDataAccess.json";
                fileShare = "connectdefinition";
                string definition = await _embeddedStorage.ReadFile("", fileName);
                await _azureStorage.SaveFile(fileShare, fileName, definition);

                fileName = "LASDataAccess.json";
                definition = await _embeddedStorage.ReadFile("", fileName);
                await _azureStorage.SaveFile(fileShare, fileName, definition);

                fileName = "CSVDataAccess.json";
                definition = await _embeddedStorage.ReadFile("", fileName);
                await _azureStorage.SaveFile(fileShare, fileName, definition);
                
                fileName = "PPDMReferenceTables.json";
                definition = await _embeddedStorage.ReadFile("", fileName);
                await _azureStorage.SaveFile(fileShare, fileName, definition);
            }
            catch (Exception ex)
            {
                Exception error = new Exception("Create DMS Model Error: ", ex);
                throw error;
            }
        }

        private async Task CreateDSMRules()
        {
            try
            {
                _logger.LogInformation($"Creating DMS rules");
                _dbConnectionString = CreateDatabaseConnectionString();
                RuleManagement rules = new RuleManagement(_azureConnectionString, _logger);
                string fileName = "StandardRules.json";
                string ruleString = await _embeddedStorage.ReadFile("", fileName);
                await rules.SaveRulesToDatabase(ruleString, _dbConnectionString);
            }
            catch (Exception ex)
            {
                Exception error = new Exception("Load DMS Rule Error: ", ex);
                throw error;
            }
        }

        private async Task CreateStoredProcedures(DataModelParameters dmParameters)
        {
            try
            {
                _logger.LogInformation($"Creating stored procedures");
                _dbConnectionString = CreateDatabaseConnectionString();
                string fileName = "StoredProcedures.sql";
                string sql = await _embeddedStorage.ReadFile("", fileName);
                string[] commandText = sql.Split(new string[] { String.Format("{0}GO{0}", Environment.NewLine) }, StringSplitOptions.RemoveEmptyEntries);
                for (int x = 0; x < commandText.Length; x++)
                {
                    if (commandText[x].Trim().Length > 0)
                    {
                        _DbStorage.ExecuteSQL(commandText[x], _dbConnectionString);
                    }
                }

                await CreateGetStoredProcedure();
                await CreateInsertStoredProcedure();
                await CreateUpdateStoredProcedure();
            }
            catch (Exception ex)
            {
                Exception error = new Exception("Create DMS Model Error: ", ex);
                throw error;
            }
        }

        private async Task CreateGetStoredProcedure()
        {
            string fileName = "DMSDataAccess.json";
            string definition = await _embeddedStorage.ReadFile("", fileName);
            List<DataAccessDef> accessDefs = JsonConvert.DeserializeObject<List<DataAccessDef>>(definition);

            string type = "Rules";
            DataAccessDef ruleDef = accessDefs.FirstOrDefault(x => x.DataType == type);
            BuildGetProcedure(type, ruleDef);
            BuildGetProcedureWithId(type, ruleDef);

            type = "Functions";
            DataAccessDef functionDef = accessDefs.FirstOrDefault(x => x.DataType == type);
            BuildGetProcedure(type, functionDef);
            BuildGetProcedureWithId(type, functionDef);

            type = "Index";
            DataAccessDef indexDef = accessDefs.FirstOrDefault(x => x.DataType == type);
            BuildGetProcedure(type, ruleDef);
            BuildGetProcedureWithQcString(indexDef);
            BuildGetProcedureWithAttributeQuery(type, "INDEXNODE", indexDef);
        }

        private void BuildGetProcedure(string dataType, DataAccessDef accessDef)
        {
            string sqlCommand = $"DROP PROCEDURE IF EXISTS spGet{dataType} ";
            _DbStorage.ExecuteSQL(sqlCommand, _dbConnectionString);

            sqlCommand = "";
            string sql = accessDef.Select;
            sqlCommand = sqlCommand + $"CREATE PROCEDURE spGet{dataType} ";
            sqlCommand = sqlCommand + " AS ";
            sqlCommand = sqlCommand + " BEGIN ";
            sqlCommand = sqlCommand + sql;
            sqlCommand = sqlCommand + " END";
            _DbStorage.ExecuteSQL(sqlCommand, _dbConnectionString);
        }
        private void BuildGetProcedureWithId(string dataType, DataAccessDef accessDef)
        {
            string sqlCommand = $"DROP PROCEDURE IF EXISTS spGetWithId{dataType} ";
            _DbStorage.ExecuteSQL(sqlCommand, _dbConnectionString);

            sqlCommand = "";
            string sql = accessDef.Select;
            string query = " WHERE ID = @id ";
            sqlCommand = sqlCommand + $"CREATE PROCEDURE spGetWithId{dataType} ";
            sqlCommand = sqlCommand + " @id INT ";
            sqlCommand = sqlCommand + " AS ";
            sqlCommand = sqlCommand + " BEGIN ";
            sqlCommand = sqlCommand + sql + query;
            sqlCommand = sqlCommand + " END";
            _DbStorage.ExecuteSQL(sqlCommand, _dbConnectionString);
        }

        private void BuildGetProcedureWithQcString(DataAccessDef accessDef)
        {
            string sqlCommand = $"DROP PROCEDURE IF EXISTS spGetWithQcStringIndex ";
            _DbStorage.ExecuteSQL(sqlCommand, _dbConnectionString);

            sqlCommand = "";
            string sql = accessDef.Select;

            sqlCommand = sqlCommand + $"CREATE PROCEDURE spGetWithQcStringIndex ";
            sqlCommand = sqlCommand + " @qcstring VARCHAR(10) ";
            sqlCommand = sqlCommand + " AS ";
            sqlCommand = sqlCommand + " BEGIN ";
            sqlCommand = sqlCommand + " declare @query as varchar(240) ";
            sqlCommand = sqlCommand + " set @query = '%' + @qcstring + ';%'";
            sqlCommand = sqlCommand + sql;
            sqlCommand = sqlCommand + " WHERE QC_STRING like @query";
            sqlCommand = sqlCommand + " END";
            _DbStorage.ExecuteSQL(sqlCommand, _dbConnectionString);
        }

        private void BuildGetProcedureWithAttributeQuery(string dataType, string attribute,
            DataAccessDef accessDef)
        {
            string sqlCommand = $"DROP PROCEDURE IF EXISTS spGet{dataType}With{attribute} ";
            _DbStorage.ExecuteSQL(sqlCommand, _dbConnectionString);

            sqlCommand = "";
            string sql = accessDef.Select;

            sqlCommand = sqlCommand + $"CREATE PROCEDURE spGet{dataType}With{attribute} ";
            sqlCommand = sqlCommand + " @query VARCHAR(max) ";
            sqlCommand = sqlCommand + " AS ";
            sqlCommand = sqlCommand + " BEGIN ";
            sqlCommand = sqlCommand + " declare @querystring as varchar(max) ";
            sqlCommand = sqlCommand + " set @querystring = @query ";
            sqlCommand = sqlCommand + sql;
            sqlCommand = sqlCommand + $" WHERE {attribute} = @querystring";
            sqlCommand = sqlCommand + " END";
            _DbStorage.ExecuteSQL(sqlCommand, _dbConnectionString);
        }

        private async Task CreateInsertStoredProcedure()
        {
            _logger.LogInformation($"Creating insert stored procedures");
            string fileName = "DMSDataAccess.json";
            string definition = await _embeddedStorage.ReadFile("", fileName);
            List<DataAccessDef> accessDefs = JsonConvert.DeserializeObject<List<DataAccessDef>>(definition);

            var dataTypes = accessDefs.Select(s => s.DataType).Where(s => s != "Index").ToList();
            foreach (string dataType in dataTypes)
            {
                DataAccessDef accessDef = accessDefs.First(x => x.DataType == dataType);
                await BuildInsertProcedure(dataType, accessDef);
            }

            string type = "Rules";
            DataAccessDef ruleDef = accessDefs.FirstOrDefault(x => x.DataType == type);
            await BuildInsertWithUDTProcedure(type, ruleDef);
            type = "Functions";
            DataAccessDef functionDef = accessDefs.FirstOrDefault(x => x.DataType == type);
            await BuildInsertProcedure(type, functionDef);
        }

        private async Task BuildInsertProcedure(string dataType, DataAccessDef accessDef)
        {
            string comma;
            string attributes;
            string sqlCommand = $"DROP PROCEDURE IF EXISTS spInsert{dataType}; ";
            sqlCommand = sqlCommand + $"DROP TYPE IF EXISTS[dbo].[UDT{dataType}];";
            _DbStorage.ExecuteSQL(sqlCommand, _dbConnectionString);

            sqlCommand = "";
            string sql = accessDef.Select;
            string[] keys = accessDef.Keys.Split(',');

            string table = GetTable(sql);
            IEnumerable<TableSchema> attributeProperties = await GetColumnInfo(table);
            string[] tableAttributes = GetAttributes(sql);
            tableAttributes = tableAttributes.Where(w => w != "Id").ToArray();

            sqlCommand = sqlCommand + $"CREATE PROCEDURE spInsert{dataType} ";
            sqlCommand = sqlCommand + " @json NVARCHAR(max) ";
            sqlCommand = sqlCommand + " AS ";
            sqlCommand = sqlCommand + " BEGIN ";

            sqlCommand = sqlCommand + $" INSERT INTO {table}";
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
                TableSchema dataProperty = attributeProperties.FirstOrDefault(x => x.COLUMN_NAME == attribute);
                attributes = attributes + comma + attribute + " " + dataProperty.GetDatabaseAttributeType() +
                    " '$." + attribute + "'";
                comma = ",";
            }
            sqlCommand = sqlCommand + attributes;
            sqlCommand = sqlCommand + ") AS jsonValues ";

            sqlCommand = sqlCommand + " END";
            _DbStorage.ExecuteSQL(sqlCommand, _dbConnectionString);
        }

        private async Task BuildInsertWithUDTProcedure(string dataType, DataAccessDef accessDef)
        {
            string sqlCommand = $"DROP PROCEDURE IF EXISTS spInsert{dataType}; ";
            sqlCommand = sqlCommand + $"DROP TYPE IF EXISTS[dbo].[UDT{dataType}];";
            _DbStorage.ExecuteSQL(sqlCommand, _dbConnectionString);

            sqlCommand = "";
            string sql = accessDef.Select;
            string table = GetTable(sql);
            IEnumerable<TableSchema> attributeProperties = await GetColumnInfo(table);
            string[] tableAttributes = GetAttributes(sql);
            tableAttributes = tableAttributes.Where(w => w != "Id").ToArray();
            CreateUserDefinedTypes(attributeProperties, sql, dataType);
            string comma = "";
            string attributes = "";
            foreach (var word in tableAttributes)
            {
                string attribute = word.Trim();
                attributes = attributes + comma + "[" + attribute + "]";
                comma = ",";
            }
            sqlCommand = sqlCommand + $"CREATE PROCEDURE [dbo].[spInsert{dataType}] @rules UDT{dataType} readonly " +
                " AS BEGIN " +
                $" INSERT INTO dbo.{table}({attributes}) " +
                $" SELECT {attributes} FROM @rules;" +
                " END";
            _DbStorage.ExecuteSQL(sqlCommand, _dbConnectionString);
        }

        private void CreateUserDefinedTypes(IEnumerable<TableSchema> attributeProperties, string sql, string dataType)
        {
            string[] tableAttributes = GetAttributes(sql);
            string comma = "";
            string attributes = "";
            foreach (var word in tableAttributes)
            {
                string attribute = word.Trim();
                TableSchema dataProperty = attributeProperties.FirstOrDefault(x => x.COLUMN_NAME == attribute);
                attributes = attributes + comma + attribute + " " + dataProperty.GetDatabaseAttributeType();
                comma = ",";
            }
            string sqlCommand = $"CREATE TYPE [dbo].[UDT{dataType}] AS TABLE ( ";
            sqlCommand = sqlCommand + attributes + ")";
            _DbStorage.ExecuteSQL(sqlCommand, _dbConnectionString);
        }

        private async Task CreateUpdateStoredProcedure()
        {
            _logger.LogInformation($"Creating update stored procedures");

            string fileName = "DMSDataAccess.json";
            string definition = await _embeddedStorage.ReadFile("", fileName);
            List<DataAccessDef> accessDefs = JsonConvert.DeserializeObject<List<DataAccessDef>>(definition);

            var dataTypes = accessDefs.Select(s => s.DataType).Where(s => s != "Index").ToList();
            foreach (string dataType in dataTypes)
            {
                DataAccessDef accessDef = accessDefs.First(x => x.DataType == dataType);
                await BuildUpdateProcedure(dataType, accessDef);
            }
            string type = "Rules";
            DataAccessDef ruleDef = accessDefs.FirstOrDefault(x => x.DataType == type);
            await BuildUpdateProcedure(type, ruleDef);
            type = "Functions";
            DataAccessDef functionDef = accessDefs.FirstOrDefault(x => x.DataType == type);
            await BuildUpdateProcedure(type, functionDef);
        }

        private async Task BuildUpdateProcedure(string dataType, DataAccessDef accessDef)
        {
            string comma;
            string attributes;
            string sqlCommand = $"DROP PROCEDURE IF EXISTS spUpdate{dataType} ";
            _DbStorage.ExecuteSQL(sqlCommand, _dbConnectionString);

            sqlCommand = "";
            string sql = accessDef.Select;
            string[] keys = accessDef.Keys.Split(',');

            string table = GetTable(sql);
            IEnumerable<TableSchema> attributeProperties = await GetColumnInfo(table);
            string[] tableAttributes = GetAttributes(sql);

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
                TableSchema dataProperty = attributeProperties.FirstOrDefault(x => x.COLUMN_NAME == attribute);
                attributes = attributes + comma + attribute + " " + dataProperty.GetDatabaseAttributeType() +
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

            _DbStorage.ExecuteSQL(sqlCommand, _dbConnectionString);
        }

        private async Task CreateFunctions()
        {
            _logger.LogInformation($"Creating functions");
            _dbConnectionString = CreateDatabaseConnectionString();

            string fileName = "Functions.sql";
            string sql = await _embeddedStorage.ReadFile("", fileName);
            string[] commandText = sql.Split(new string[] { String.Format("{0}GO{0}", Environment.NewLine) }, StringSplitOptions.RemoveEmptyEntries);
            for (int x = 0; x < commandText.Length; x++)
            {
                if (commandText[x].Trim().Length > 0)
                {
                    _DbStorage.ExecuteSQL(commandText[x], _dbConnectionString);
                }
            }
        }

        private string GetTable(string select)
        {
            select = select.ToUpper();
            int from = select.IndexOf(" FROM ") + 6;
            string table = select.Substring(from);
            return table;
        }

        private Task<IEnumerable<TableSchema>> GetColumnInfo(string table) =>
            _DbStorage.LoadData<TableSchema, dynamic>("dbo.sp_columns", new { TABLE_NAME = table }, _dbConnectionString);

        private string[] GetAttributes(string select)
        {
            int from = 7;
            int to = select.IndexOf("from");
            int length = to - 8;
            string attributes = select.Substring(from, length);
            string[] words = attributes.Split(',');

            return words;
        }

        private string CreateDatabaseConnectionString()
        {
            string connectionString = string.Empty;
            if (String.IsNullOrEmpty(_connector.ConnectionString)) 
            {
                string source = $"Source={_connector.DatabaseServer};";
                string database = $"Initial Catalog ={_connector.Catalog};";
                string timeout = "Connection Timeout=120";
                string persistSecurity = "Persist Security Info=False;";
                string multipleActive = "MultipleActiveResultSets=True;";
                string integratedSecurity = "";
                string user = "";
                //Encryption is currently not used, more testing later
                //string encryption = "Encrypt=True;TrustServerCertificate=False;";
                string encryption = "Encrypt=False;";
                if (!string.IsNullOrWhiteSpace(_connector.User))
                    user = $"User ID={_connector.User};";
                else
                    integratedSecurity = "Integrated Security=True;";
                string password = "";
                if (!string.IsNullOrWhiteSpace(_connector.Password)) password = $"Password={_connector.Password};";

                connectionString = "Data " + source + persistSecurity + database +
                    user + password + integratedSecurity + encryption + multipleActive;

                connectionString = connectionString + timeout;
            }
            else
            {
                connectionString = _connector.ConnectionString;
            }
            
            return connectionString;
        }

        private async Task PopulateFixedKeys()
        {
            string fileName = "PPDMReferenceTables.json";
            string referenceJson = await _embeddedStorage.ReadFile("", fileName);
            List<ReferenceTable> references = JsonConvert.DeserializeObject<List<ReferenceTable>>(referenceJson);
            foreach (ReferenceTable reference in references)
            {
                if (!string.IsNullOrEmpty(reference.FixedKey))
                {
                    string[] fixedKey = reference.FixedKey.Split('=');
                    string column = fixedKey[0].Trim();
                    string columnValue = fixedKey[1].Trim();
                    IEnumerable<ForeignKeyInfo> fkInfo = await GetForeignKeyInfo(reference.Table, column);
                    if (fkInfo.Count() == 1)
                    {
                        ForeignKeyInfo info = fkInfo.First();
                        string select = $"Select * from {info.ReferencedTable} ";
                        string query = $"where {info.ReferencedColumn} = '{columnValue}'";
                        DataTable refTable = _DbStorage.GetDataTable(select, _dbConnectionString);
                        if (refTable.Rows.Count == 0)
                        {
                            string sql = $"insert into {info.ReferencedTable} ({info.ReferencedColumn}) Values ('{columnValue}')";
                            _DbStorage.ExecuteSQL(sql, _dbConnectionString);
                        }
                    }
                    else
                    {
                        throw new NullReferenceException("Serious problems with getting the foreign key info");
                    }

                }
            }
        }

        public async Task<IEnumerable<ForeignKeyInfo>> GetForeignKeyInfo(string table, string column)
        {
            string sql = "SELECT obj.name AS FkName, sch.name AS[SchemaName], " +
                        "tab1.name AS[Table], col1.name AS[Column], tab2.name AS[ReferencedTable], col2.name AS[ReferencedColumn] " +
                        "FROM sys.foreign_key_columns fkc " +
                        "INNER JOIN sys.objects obj ON obj.object_id = fkc.constraint_object_id " +
                        "INNER JOIN sys.tables tab1 ON tab1.object_id = fkc.parent_object_id " +
                        "INNER JOIN sys.schemas sch ON tab1.schema_id = sch.schema_id " +
                        "INNER JOIN sys.columns col1 ON col1.column_id = parent_column_id AND col1.object_id = tab1.object_id " +
                        "INNER JOIN sys.tables tab2 ON tab2.object_id = fkc.referenced_object_id " +
                        "INNER JOIN sys.columns col2 ON col2.column_id = referenced_column_id AND col2.object_id = tab2.object_id " +
                        $"where tab1.name = '{table}' and col1.name = '{column}'";
            IEnumerable<ForeignKeyInfo> result = await _DbStorage.ReadData<ForeignKeyInfo>(sql, _dbConnectionString);
            if (result == null)
            {
                throw new ArgumentException("Table does not exist");
            }
            return result;
        }
    }
}
