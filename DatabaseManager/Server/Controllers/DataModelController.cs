using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using DatabaseManager.Server.Helpers;
using DatabaseManager.Shared;
using Microsoft.Azure.Storage;
using Microsoft.Azure.Storage.File;
using Microsoft.AspNetCore.Hosting;
using DatabaseManager.Server.Entities;
using System.Data;
using Newtonsoft.Json;

namespace DatabaseManager.Server.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class DataModelController : ControllerBase
    {
        private readonly IWebHostEnvironment _env;
        private readonly string connectionString;
        private readonly string _contentRootPath;
        private readonly string container = "sources";

        public DataModelController(IConfiguration configuration, IWebHostEnvironment env)
        {
            connectionString = configuration.GetConnectionString("AzureStorageConnection");
            _env = env;
            _contentRootPath = _env.ContentRootPath;
        }

        [HttpPost]
        public async Task<ActionResult<string>> DataModelCreate(DataModelParameters dmParameters)
        {
            if (dmParameters == null) return BadRequest();

            try
            {
                ConnectParameters connector = Common.GetConnectParameters(connectionString, container, dmParameters.DataConnector);
                if (connector == null) return BadRequest();
                if (dmParameters.ModelOption == "PPDM Model")
                {
                    CreatePPDMModel(dmParameters, connector);
                }
                else if (dmParameters.ModelOption == "DMS Model")
                {
                    CreateDMSModel(connector);
                }
                else if (dmParameters.ModelOption == "DMS Rules")
                {
                    CreateDSMRules(connector);
                }
                else if (dmParameters.ModelOption == "Stored Procedures")
                {
                    CreateStoredProcedures(connector);
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

        private void CreateStoredProcedures(ConnectParameters connector)
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

                CreateInsertStoredProcedure(dbConn);
                CreateUpdateStoredProcedure(dbConn);
                
                dbConn.CloseConnection();
            }
            catch (Exception ex)
            {
                Exception error = new Exception("Create DMS Model Error: ", ex);
                throw error;
            }
        }

        private void CreateInsertStoredProcedure(DbUtilities dbConn)
        {
            string comma;
            string attributes;

            List<DataAccessDef> accessDefs = Common.GetDataAccessDefinition(_env);
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

        private void CreateUpdateStoredProcedure(DbUtilities dbConn)
        {
            string comma;
            string attributes;

            List<DataAccessDef> accessDefs = Common.GetDataAccessDefinition(_env);
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

        private void CreateDMSModel(ConnectParameters connector)
        {
            try
            {
                string sqlFile = _contentRootPath + @"\DataBase\DataScienceManagement.sql";
                string sql = System.IO.File.ReadAllText(sqlFile);
                DbUtilities dbConn = new DbUtilities();
                dbConn.OpenConnection(connector);
                dbConn.SQLExecute(sql);
                dbConn.CloseConnection();
            }
            catch (Exception ex)
            {
                Exception error = new Exception("Create DMS Model Error: ", ex);
                throw error;
            }
        }

        private void CreateDSMRules(ConnectParameters connector)
        {
            try
            {
                DbUtilities dbConn = new DbUtilities();
                dbConn.OpenConnection(connector);
                RuleUtilities.SaveRulesFile(dbConn, _contentRootPath);
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

        private void CreatePPDMModel(DataModelParameters dmParameters, ConnectParameters connector)
        {
            try
            {
                CloudStorageAccount account = CloudStorageAccount.Parse(connectionString);
                CloudFileClient fileClient = account.CreateCloudFileClient();
                CloudFileShare share = fileClient.GetShareReference(dmParameters.FileShare);
                if (share.Exists())
                {
                    CloudFileDirectory rootDir = share.GetRootDirectoryReference();
                    CloudFile file = rootDir.GetFileReference(dmParameters.FileName);
                    if (file.Exists())
                    {
                        string sql = file.DownloadTextAsync().Result;
                        DbUtilities dbConn = new DbUtilities();
                        dbConn.OpenConnection(connector);
                        dbConn.SQLExecute(sql);
                        dbConn.CloseConnection();
                    }
                }
            }
            catch (Exception ex)
            {
                Exception error = new Exception("Create PPDM Model Error: ", ex);
                throw error;
            }
        }
    }
}