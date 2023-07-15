using DatabaseManager.Services.Index.Models;
using DatabaseManager.Services.Index.Services;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DatabaseManager.Services.Index.Helpers
{
    public class CreateIndexDataModel
    {
        private string _dbConnectionString;
        private readonly IFileStorageService _embeddedStorage;
        private readonly IDataAccess _dba;

        public CreateIndexDataModel()
        {
            _embeddedStorage = new EmbeddedFileStorageService();
            _dba = new DBDataAccess();
        }

        public async Task CreateDMSModel(string connectionString)
        {
            try
            {
                string dropSql = "DROP TABLE IF EXISTS ";
                string createSql = "CREATE TABLE ";

                string sql = dropSql + "pdo_qc_index";
                await _dba.ExecuteSQL(sql, connectionString);
                sql = createSql + "pdo_qc_index" +
                    "(" +
                    "IndexNode hierarchyid PRIMARY KEY CLUSTERED," +
                    "IndexLevel AS IndexNode.GetLevel()," +
                    "IndexID int IDENTITY(1,1) UNIQUE," +
                    "DataName NVARCHAR(40) NOT NULL," +
                    "DataType NVARCHAR(40) NULL," +
                    "DataKey NVARCHAR(400) NULL," +
                    "QC_LOCATION sys.geography," +
                    "Latitude NUMERIC(14,9)," +
                    "Longitude NUMERIC(14,9)," +
                    "UniqKey NVARCHAR(100)," +
                    "JsonDataObject NVARCHAR(max)," +
                    "QC_STRING NVARCHAR(400)" +
                    ")";
                await _dba.ExecuteSQL(sql, connectionString);

                sql = "CREATE UNIQUE INDEX QCINDEX ON pdo_qc_index(IndexLevel, IndexNode)";
                await _dba.ExecuteSQL(sql, connectionString);
            }
            catch (Exception ex)
            {
                Exception error = new Exception("Create DMS Model Error: ", ex);
                throw error;
            }
        }
        public async Task CreateIndexStoredProcedures(string connectionString, string indexSql)
        {
            _dbConnectionString = connectionString;
            string fileName = "StoredProcedures.sql";
            string sql = await _embeddedStorage.ReadFile("", fileName);
            string[] commandText = sql.Split(new string[] { String.Format("{0}GO{0}", Environment.NewLine) }, StringSplitOptions.RemoveEmptyEntries);
            for (int x = 0; x < commandText.Length; x++)
            {
                if (commandText[x].Trim().Length > 0)
                {
                    await _dba.ExecuteSQL(commandText[x], connectionString);
                }
            }
            await CreateGetStoredProcedure(indexSql);
        }

        private async Task CreateGetStoredProcedure(string indexSql)
        {
            string type = "Index";
            BuildGetProcedure(type, indexSql);
            BuildGetProcedureWithQcString(indexSql);
            BuildGetProcedureWithAttributeQuery(type, "INDEXNODE", indexSql);
        }

        private void BuildGetProcedureWithAttributeQuery(string dataType, string attribute,
            string indexSql)
        {
            string sqlCommand = $"DROP PROCEDURE IF EXISTS spGet{dataType}With{attribute} ";
            _dba.ExecuteSQL(sqlCommand, _dbConnectionString);

            sqlCommand = "";
            string sql = indexSql;

            sqlCommand = sqlCommand + $"CREATE PROCEDURE spGet{dataType}With{attribute} ";
            sqlCommand = sqlCommand + " @query VARCHAR(max) ";
            sqlCommand = sqlCommand + " AS ";
            sqlCommand = sqlCommand + " BEGIN ";
            sqlCommand = sqlCommand + " declare @querystring as varchar(max) ";
            sqlCommand = sqlCommand + " set @querystring = @query ";
            sqlCommand = sqlCommand + sql;
            sqlCommand = sqlCommand + $" WHERE {attribute} = @querystring";
            sqlCommand = sqlCommand + " END";
            _dba.ExecuteSQL(sqlCommand, _dbConnectionString);
        }

        private void BuildGetProcedureWithQcString(string indexSql)
        {
            string sqlCommand = $"DROP PROCEDURE IF EXISTS spGetWithQcStringIndex ";
            _dba.ExecuteSQL(sqlCommand, _dbConnectionString);

            sqlCommand = "";
            string sql = indexSql;

            sqlCommand = sqlCommand + $"CREATE PROCEDURE spGetWithQcStringIndex ";
            sqlCommand = sqlCommand + " @qcstring VARCHAR(10) ";
            sqlCommand = sqlCommand + " AS ";
            sqlCommand = sqlCommand + " BEGIN ";
            sqlCommand = sqlCommand + " declare @query as varchar(240) ";
            sqlCommand = sqlCommand + " set @query = '%' + @qcstring + ';%'";
            sqlCommand = sqlCommand + sql;
            sqlCommand = sqlCommand + " WHERE QC_STRING like @query";
            sqlCommand = sqlCommand + " END";
            _dba.ExecuteSQL(sqlCommand, _dbConnectionString);
        }

        private void BuildGetProcedure(string dataType, string indexSql)
        {
            string sqlCommand = $"DROP PROCEDURE IF EXISTS spGet{dataType} ";
            _dba.ExecuteSQL(sqlCommand, _dbConnectionString);

            sqlCommand = "";
            string sql = indexSql;
            sqlCommand = sqlCommand + $"CREATE PROCEDURE spGet{dataType} ";
            sqlCommand = sqlCommand + " AS ";
            sqlCommand = sqlCommand + " BEGIN ";
            sqlCommand = sqlCommand + sql;
            sqlCommand = sqlCommand + " END";
            _dba.ExecuteSQL(sqlCommand, _dbConnectionString);
        }
    }
}
