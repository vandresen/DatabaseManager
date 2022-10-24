using DatabaseManager.Common.DBAccess;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DatabaseManager.Common.Initializer
{
    public class DbInitializer : IDbInitializer
    {
        private readonly IADODataAccess _db;

        public DbInitializer(IADODataAccess db)
        {
            _db = db;
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
