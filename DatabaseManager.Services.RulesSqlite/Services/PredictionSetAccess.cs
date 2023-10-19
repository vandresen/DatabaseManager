using DatabaseManager.Services.RulesSqlite.Models;
using System.Data;
using System.Security.Cryptography;

namespace DatabaseManager.Services.RulesSqlite.Services
{
    public class PredictionSetAccess : IPredictionSetAccess
    {
        private readonly string _databaseFile = @".\mydatabase.db";
        private string _connectionString;
        private readonly ILogger<PredictionSetAccess> _log;
        private readonly IDataAccess _id;
        private string _getSql;
        private string _table = "pdo_rule_predictionsets";
        private string _selectAttributes = "Id, Name, Description, RuleSet";

        public PredictionSetAccess(ILogger<PredictionSetAccess> log, IDataAccess id)
        {
            _connectionString = @"Data Source=" + _databaseFile;
            _log = log;
            _id = id;
            _getSql = "Select " + _selectAttributes + " From " + _table;
        }

        public async Task DeletePredictionDataSet(int id)
        {
            string sql = $"DELETE FROM {_table} WHERE Id = {id}";
            await _id.ExecuteSQL(sql, _connectionString);
        }

        public async Task<PredictionSet> GetPredictionDataSet(string name)
        {
            string sql = _getSql + $" WHERE Name = '{name}'";
            var results = await _id.ReadData<PredictionSet>(sql, _connectionString);
            return results.FirstOrDefault();
        }

        public async Task<IEnumerable<PredictionSet>> GetPredictionDataSets()
        {
            IEnumerable<PredictionSet> result = await _id.ReadData<PredictionSet>(_getSql, _connectionString);
            return result;
        }

        public async Task SavePredictionDataSet(PredictionSet predictionSet)
        {
            string sql = $"INSERT INTO {_table} " +
                "(Name, Description, RuleSet) " +
                "VALUES(@Name, @Description, @RuleSet)";
            await _id.InsertUpdateData(sql, predictionSet, _connectionString);
        }

        public async Task UpdatePredictionDataSet(PredictionSet predictionSet)
        {
            string sql = $"UPDATE {_table} SET " +
                "Name=@Name, Description=@Description, RuleSet=@RuleSet " +
                $"WHERE Id = {predictionSet.Id}";
            await _id.InsertUpdateData(sql, predictionSet, _connectionString);
        }

    }
}
