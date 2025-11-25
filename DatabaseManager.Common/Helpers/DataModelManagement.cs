using DatabaseManager.Common.Data;
using DatabaseManager.Common.DBAccess;
using DatabaseManager.Common.Entities;
using DatabaseManager.Common.Extensions;
using DatabaseManager.Common.Initializer;
using DatabaseManager.Common.Services;
using DatabaseManager.Shared;
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json;
using System.Data;
using static DatabaseManager.Common.Data.SystemDBData;

namespace DatabaseManager.Common.Helpers
{
    public class DataModelManagement
    {
        private readonly string fileShare = "database";
        private readonly string azureConnectionString;
        private readonly string _sqlRootPath;
        private readonly IFileStorageServiceCommon _fileStorage;
        private readonly IIndexDBAccess _indexData;
        private readonly IDapperDataAccess _dp;
        private readonly ISystemData _systemData;
        private readonly IDbInitializer _dbi;
        private readonly IADODataAccess _db;

        public DataModelManagement(string azureConnectionString, string sqlRootPath)
        {
            _sqlRootPath = sqlRootPath;
            this.azureConnectionString = azureConnectionString;
            var builder = new ConfigurationBuilder();
            IConfiguration configuration = builder.Build();
            _fileStorage = new AzureFileStorageServiceCommon(configuration);
            _fileStorage.SetConnectionString(azureConnectionString);
            _dp = new DapperDataAccess();
            _indexData = new IndexDBAccess();
            _systemData = new SystemDBData(_dp);
            _db = new ADODataAccess();
            _dbi = new DbInitializer(_db);
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
                RuleManagement rules = new RuleManagement(azureConnectionString);
                List<DataAccessDef> accessDefs = await GetDataAccessDefinitions();
                string ruleString = await ReadDatabaseFile("StandardRules.json");
                await rules.SaveRulesToDatabase(ruleString, connector);
            }
            catch (Exception ex)
            {
                Exception error = new Exception("Load DMS Rule Error: ", ex);
                throw error;
            }
        }


        private async Task CreateStoredProcedures(ConnectParameters connector)
        {
            try
            {
                string sql = await ReadDatabaseFile("StoredProcedures.sql");
                string[] commandText = sql.Split(new string[] { String.Format("{0}GO{0}", Environment.NewLine) }, StringSplitOptions.RemoveEmptyEntries);
                for (int x = 0; x < commandText.Length; x++)
                {
                    if (commandText[x].Trim().Length > 0)
                    {
                        _db.ExecuteSQL(commandText[x], connector.ConnectionString);
                    }
                }

                CreateGetStoredProcedure(connector.ConnectionString);
                await CreateInsertStoredProcedure(connector.ConnectionString);
                await CreateUpdateStoredProcedure(connector.ConnectionString);
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
            for (int x = 0; x < commandText.Length; x++)
            {
                if (commandText[x].Trim().Length > 0)
                {
                    _db.ExecuteSQL(commandText[x], connector.ConnectionString);
                }
            }
        }

        private void CreateUserDefinedTypes(IEnumerable<TableSchema> attributeProperties, string sql, string dataType, string connectionString)
        {
            string[] tableAttributes = Common.GetAttributes(sql);
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

            _db.ExecuteSQL(sqlCommand, connectionString);
        }

        private async Task CreateDMSModel(DataModelParameters dmParameters, ConnectParameters connector)
        {
            try
            {
                _dbi.CreateDatabaseManagementTables(connector.ConnectionString);
                _dbi.InitializeInternalRuleFunctions(connector.ConnectionString);

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

        private void CreateGetStoredProcedure(string connectionString)
        {
            RuleManagement rm = new RuleManagement();
            string type = "Rules";
            DataAccessDef ruleDef = rm.GetDataAccessDefinition(type);
            BuildGetProcedure(type, ruleDef, connectionString);
            BuildGetProcedureWithId(type, ruleDef, connectionString);
            type = "Functions";
            DataAccessDef functionDef = rm.GetDataAccessDefinition(type);
            BuildGetProcedure(type, functionDef, connectionString);
            BuildGetProcedureWithId(type, functionDef, connectionString);

            type = "Index";
            DataAccessDef indexDef = _indexData.GetDataAccessDefinition();
            BuildGetProcedure(type, indexDef, connectionString);
            BuildGetProcedureWithQcString(indexDef, connectionString);
            BuildGetProcedureWithAttributeQuery(type, "INDEXNODE", indexDef, connectionString);
        }

        private void BuildGetProcedure(string dataType, DataAccessDef accessDef, string connectionString)
        {
            string sqlCommand = $"DROP PROCEDURE IF EXISTS spGet{dataType} ";
            _db.ExecuteSQL(sqlCommand, connectionString);

            sqlCommand = "";
            string sql = accessDef.Select;
            sqlCommand = sqlCommand + $"CREATE PROCEDURE spGet{dataType} ";
            sqlCommand = sqlCommand + " AS ";
            sqlCommand = sqlCommand + " BEGIN ";
            sqlCommand = sqlCommand + sql;
            sqlCommand = sqlCommand + " END";
            _db.ExecuteSQL(sqlCommand, connectionString);
        }

        private void BuildGetProcedureWithId(string dataType, DataAccessDef accessDef, string connectionString)
        {
            string sqlCommand = $"DROP PROCEDURE IF EXISTS spGetWithId{dataType} ";
            _db.ExecuteSQL(sqlCommand, connectionString);

            sqlCommand = "";
            string sql = accessDef.Select;
            string query = " WHERE ID = @id ";
            sqlCommand = sqlCommand + $"CREATE PROCEDURE spGetWithId{dataType} ";
            sqlCommand = sqlCommand + " @id INT ";
            sqlCommand = sqlCommand + " AS ";
            sqlCommand = sqlCommand + " BEGIN ";
            sqlCommand = sqlCommand + sql + query;
            sqlCommand = sqlCommand + " END";
            _db.ExecuteSQL(sqlCommand, connectionString);
        }

        private void BuildGetProcedureWithQcString(DataAccessDef accessDef, string connectionString)
        {
            string sqlCommand = $"DROP PROCEDURE IF EXISTS spGetWithQcStringIndex ";
            _db.ExecuteSQL(sqlCommand, connectionString);

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
            _db.ExecuteSQL(sqlCommand, connectionString);
        }

        private void BuildGetProcedureWithAttributeQuery(string dataType, string attribute,
            DataAccessDef accessDef, string connectionString)
        {
            string sqlCommand = $"DROP PROCEDURE IF EXISTS spGet{dataType}With{attribute} ";
            _db.ExecuteSQL(sqlCommand, connectionString);

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
            _db.ExecuteSQL(sqlCommand, connectionString);
        }

        private async Task CreateInsertStoredProcedure(string connectionString)
        {
            List<DataAccessDef> accessDefs = await GetDataAccessDefinitions();
            var dataTypes = accessDefs.Select(s => s.DataType).Where(s => s != "Index").ToList();
            foreach(string dataType in dataTypes)
            {
                DataAccessDef accessDef = accessDefs.First(x => x.DataType == dataType);
                await BuildInsertProcedure(dataType, accessDef, connectionString);
            }
            RuleManagement rm = new RuleManagement();
            string type = "Rules";
            DataAccessDef ruleDef = rm.GetDataAccessDefinition(type);
            await BuildInsertWithUDTProcedure(type, ruleDef, connectionString);
            type = "Functions";
            DataAccessDef functionDef = rm.GetDataAccessDefinition(type);
            await BuildInsertProcedure(type, functionDef, connectionString);
        }

        private async Task BuildInsertWithUDTProcedure(string dataType, DataAccessDef accessDef, string connectionString)
        {
            string sqlCommand = $"DROP PROCEDURE IF EXISTS spInsert{dataType}; ";
            sqlCommand = sqlCommand + $"DROP TYPE IF EXISTS[dbo].[UDT{dataType}];";
            _db.ExecuteSQL(sqlCommand, connectionString);

            sqlCommand = "";
            string sql = accessDef.Select;
            string table = Common.GetTable(sql);
            IEnumerable<TableSchema> attributeProperties = await _systemData.GetColumnInfo(connectionString, table);
            string[] tableAttributes = Common.GetAttributes(sql);
            tableAttributes = tableAttributes.Where(w => w != "Id").ToArray();
            CreateUserDefinedTypes(attributeProperties, sql, dataType, connectionString);
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
            _db.ExecuteSQL(sqlCommand, connectionString);
        }

        private async Task BuildInsertProcedure(string dataType, DataAccessDef accessDef, string connectionString)
        {
            string comma;
            string attributes;
            string sqlCommand = $"DROP PROCEDURE IF EXISTS spInsert{dataType}; ";
            sqlCommand = sqlCommand + $"DROP TYPE IF EXISTS[dbo].[UDT{dataType}];";
            _db.ExecuteSQL(sqlCommand, connectionString);

            sqlCommand = "";
            string sql = accessDef.Select;
            string[] keys = accessDef.Keys.Split(',');

            string table = Common.GetTable(sql);
            IEnumerable<TableSchema> attributeProperties = await _systemData.GetColumnInfo(connectionString, table);
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
                TableSchema dataProperty = attributeProperties.FirstOrDefault(x => x.COLUMN_NAME == attribute);
                attributes = attributes + comma + attribute + " " + dataProperty.GetDatabaseAttributeType() +
                    " '$." + attribute + "'";
                comma = ",";
            }
            sqlCommand = sqlCommand + attributes;
            sqlCommand = sqlCommand + ") AS jsonValues ";

            sqlCommand = sqlCommand + " END";
            _db.ExecuteSQL(sqlCommand, connectionString);
        }

        private async Task CreateUpdateStoredProcedure(string connectionString)
        {
            List<DataAccessDef> accessDefs = await GetDataAccessDefinitions();
            var dataTypes = accessDefs.Select(s => s.DataType).Where(s => s != "Index").ToList();
            foreach (string dataType in dataTypes)
            {
                DataAccessDef accessDef = accessDefs.First(x => x.DataType == dataType);
                await BuildUpdateProcedure(dataType, accessDef, connectionString);
            }
            RuleManagement rm = new RuleManagement();
            string type = "Rules";
            DataAccessDef ruleDef = rm.GetDataAccessDefinition(type);
            await BuildUpdateProcedure(type, ruleDef, connectionString);
            type = "Functions";
            DataAccessDef functionDef = rm.GetDataAccessDefinition(type);
            await BuildUpdateProcedure(type, functionDef, connectionString);
        }

        private async Task BuildUpdateProcedure(string dataType, DataAccessDef accessDef, string connectionString)
        {
            string comma;
            string attributes;
            string sqlCommand = $"DROP PROCEDURE IF EXISTS spUpdate{dataType} ";
            _db.ExecuteSQL(sqlCommand, connectionString);

            sqlCommand = "";
            string sql = accessDef.Select;
            string[] keys = accessDef.Keys.Split(',');

            string table = Common.GetTable(sql);
            IEnumerable<TableSchema> attributeProperties = await _systemData.GetColumnInfo(connectionString, table);
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

            _db.ExecuteSQL(sqlCommand, connectionString);
        }
        
        private async Task CreatePpdmModifications(ConnectParameters connector)
        {
            try
            {
                string sql = await ReadDatabaseFile("PpdmModifications.sql");
                string[] commandText = sql.Split(new string[] { String.Format("{0}GO{0}", Environment.NewLine) }, StringSplitOptions.RemoveEmptyEntries);
                await PopulateFixedKeys(connector);
                for (int x = 0; x < commandText.Length; x++)
                {
                    if (commandText[x].Trim().Length > 0)
                    {
                        _db.ExecuteSQL(commandText[x], connector.ConnectionString);
                    }
                }

                var lithologies = Enum.GetValues(typeof(LithologyInfo.Lithology))
                    .Cast<LithologyInfo.Lithology>()
                    .Select(l => l.ToString())
                    .ToList();
                foreach (var litho in lithologies)
                {
                    sql = $@"IF NOT EXISTS (SELECT 1 FROM r_lithology WHERE LITHOLOGY = '{litho}')" +
                        $"BEGIN INSERT INTO r_lithology (LITHOLOGY, LONG_NAME) VALUES ('{litho}', '{litho}') END";
                    _db.ExecuteSQL(sql, connector.ConnectionString);
                }
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
                    _db.ExecuteSQL(sql, connector.ConnectionString);
                }
            }
            catch (Exception ex)
            {
                Exception error = new Exception("Create PPDM Model Error: ", ex);
                throw error;
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

        private async Task PopulateFixedKeys(ConnectParameters connector)
        {
            string referenceJson = await _fileStorage.ReadFile("connectdefinition", "PPDMReferenceTables.json");
            List<ReferenceTable> references = JsonConvert.DeserializeObject<List<ReferenceTable>>(referenceJson);
            foreach (ReferenceTable reference  in references)
            {
                if (!string.IsNullOrEmpty(reference.FixedKey))
                {
                    string[] fixedKey = reference.FixedKey.Split('=');
                    string column = fixedKey[0].Trim();
                    string columnValue = fixedKey[1].Trim();
                    SystemDBData systemData = new SystemDBData(_dp);
                    IEnumerable<ForeignKeyInfo> fkInfo = await systemData.GetForeignKeyInfo(connector.ConnectionString, reference.Table, column);
                    if (fkInfo.Count() == 1)
                    {
                        IDataAccess dbData = new DBDataAccess(_db);
                        dbData.OpenConnection(connector, null);
                        ForeignKeyInfo info = fkInfo.First();
                        string select = $"Select * from {info.ReferencedTable} ";
                        string query = $"where {info.ReferencedColumn} = '{columnValue}'";
                        DataTable refTable = await dbData.GetDataTable(select, query, "");
                        if (refTable.Rows.Count == 0)
                        {
                            string sql = $"insert into {info.ReferencedTable} ({info.ReferencedColumn}) Values ('{columnValue}')";
                            _db.ExecuteSQL(sql, connector.ConnectionString);
                        }
                    }
                    else
                    {
                        throw new NullReferenceException("Serious problems with getting the foreign key info");
                    }
                }
            }
        }
    }
}
