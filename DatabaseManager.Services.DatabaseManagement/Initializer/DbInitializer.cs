using DatabaseManager.Services.DatabaseManagement.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DatabaseManager.Services.DatabaseManagement.Initializer
{
    public class DbInitializer : IDbInitializer
    {
        private readonly IDatabaseAccessService _db;

        public DbInitializer(IDatabaseAccessService db)
        {
            _db = db;
        }

        public void CreateDatabaseManagementTables(string connectionString)
        {
            string dropSql = "DROP TABLE IF EXISTS ";
            string createSql = "CREATE TABLE ";

            string sql = dropSql + "pdo_qc_index";
            _db.ExecuteSQL(sql, connectionString);
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
            _db.ExecuteSQL(sql, connectionString);

            sql = dropSql + "pdo_qc_rules";
            _db.ExecuteSQL(sql, connectionString);
            sql = createSql + "pdo_qc_rules" +
                "(" +
                "Id INT IDENTITY(1,1) PRIMARY KEY," +
                "Active NVARCHAR(1) NULL," +
                "DataType NVARCHAR(40) NOT NULL," +
                "DataAttribute NVARCHAR(255) NULL," +
                "RuleType NVARCHAR(40) NOT NULL," +
                "RuleName NVARCHAR(40) NOT NULL," +
                "RuleDescription NVARCHAR(255) NULL," +
                "RuleFunction NVARCHAR(255) NULL," +
                "RuleKey NVARCHAR(16) NULL," +
                "RuleParameters NVARCHAR(500) NULL," +
                "RuleFilter NVARCHAR(255) NULL," +
                "FailRule NVARCHAR(255) NULL," +
                "PredictionOrder int NULL," +
                "CreatedBy NVARCHAR(255) NULL," +
                "CreatedDate datetime NULL," +
                "ModifiedBy NVARCHAR(255) NULL," +
                "ModifiedDate datetime NULL," +
                "KeyNumber int NOT NULL" +
                ")";
            _db.ExecuteSQL(sql, connectionString);

            sql = dropSql + "pdo_rule_functions";
            _db.ExecuteSQL(sql, connectionString);
            sql = createSql + "pdo_rule_functions" +
                "(" +
                "Id INT IDENTITY(1,1) PRIMARY KEY," +
                "FunctionName NVARCHAR(255) NOT NULL," +
                "FunctionUrl NVARCHAR(255) NOT NULL," +
                "FunctionType NVARCHAR(1)," +
                "FunctionKey NVARCHAR(255)" +
                ")";
            _db.ExecuteSQL(sql, connectionString);

            sql = "CREATE UNIQUE INDEX QCINDEX ON pdo_qc_index(IndexLevel, IndexNode)";
            _db.ExecuteSQL(sql, connectionString);
        }

        public void InitializeInternalRuleFunctions(string connectionString)
        {
            string baseSql = @"INSERT INTO pdo_rule_functions";
            string sql = baseSql + @"(FunctionName, FunctionUrl) VALUES('Completeness', 'Completeness')";
            _db.ExecuteSQL(sql, connectionString);
            sql = baseSql + "(FunctionName, FunctionUrl, FunctionType) VALUES('DeleteDataObject', 'DeleteDataObject', 'P')";
            _db.ExecuteSQL(sql, connectionString);
            sql = baseSql + "(FunctionName, FunctionUrl) VALUES('Uniqueness', 'Uniqueness')";
            _db.ExecuteSQL(sql, connectionString);
            sql = baseSql + "(FunctionName, FunctionUrl) VALUES('Entirety', 'Entirety')";
            _db.ExecuteSQL(sql, connectionString);
            sql = baseSql + "(FunctionName, FunctionUrl) VALUES('Consistency', 'Consistency')";
            _db.ExecuteSQL(sql, connectionString);
            sql = baseSql + "(FunctionName, FunctionUrl, FunctionType) VALUES('ValidityRange', 'ValidityRange', 'V')";
            _db.ExecuteSQL(sql, connectionString);
            sql = baseSql + "(FunctionName, FunctionUrl, FunctionType) VALUES('CurveSpikes', 'CurveSpikes', 'V')";
            _db.ExecuteSQL(sql, connectionString);
            sql = baseSql + "(FunctionName, FunctionUrl, FunctionType) VALUES('IsNumber', 'IsNumber', 'V')";
            _db.ExecuteSQL(sql, connectionString);
            sql = baseSql + "(FunctionName, FunctionUrl, FunctionType) VALUES('StringLength', 'StringLength', 'V')";
            _db.ExecuteSQL(sql, connectionString);
            sql = baseSql + "(FunctionName, FunctionUrl, FunctionType) VALUES('IsEqualTo', 'IsEqualTo', 'V')";
            _db.ExecuteSQL(sql, connectionString);
            sql = baseSql + "(FunctionName, FunctionUrl, FunctionType) VALUES('IsGreaterThan', 'IsGreaterThan', 'V')";
            _db.ExecuteSQL(sql, connectionString);
            sql = baseSql + "(FunctionName, FunctionUrl, FunctionType) VALUES('PredictDepthUsingIDW', 'PredictDepthUsingIDW', 'P')";
            _db.ExecuteSQL(sql, connectionString);
            sql = baseSql + "(FunctionName, FunctionUrl, FunctionType) VALUES('PredictDominantLithology', 'PredictDominantLithology', 'P')";
            _db.ExecuteSQL(sql, connectionString);
            sql = baseSql + "(FunctionName, FunctionUrl, FunctionType) VALUES('PredictFormationOrder', 'PredictFormationOrder', 'P')";
            _db.ExecuteSQL(sql, connectionString);
            sql = baseSql + "(FunctionName, FunctionUrl, FunctionType) VALUES('PredictLogDepthAttributes', 'PredictLogDepthAttributes', 'P')";
            _db.ExecuteSQL(sql, connectionString);
            sql = baseSql + "(FunctionName, FunctionUrl, FunctionType) VALUES('PredictMissingDataObjects', 'PredictMissingDataObjects', 'P')";
            _db.ExecuteSQL(sql, connectionString);
        }
    }
}
