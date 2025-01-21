using DatabaseManager.Common.DBAccess;
using DatabaseManager.Common.Entities;
using DatabaseManager.Shared;
using Newtonsoft.Json;

namespace DatabaseManager.Common.Data
{
    public class RuleData : IRuleData
    {
        private readonly IDapperDataAccess _dp;
        private readonly IADODataAccess _db;

        public RuleData(IDapperDataAccess dp)
        {
            _dp = dp;
        }

        public RuleData(IDapperDataAccess dp, IADODataAccess db)
        {
            _dp = dp;
            _db = db;
        }

        public Task<IEnumerable<RuleModel>> GetRulesFromSP(string connectionString) =>
            _dp.LoadData<RuleModel, dynamic>("dbo.spGetRules", new { }, connectionString);

        public async Task<RuleModel> GetRuleFromSP(int id, string connectionString)
        {
            var results = await _dp.LoadData<RuleModel, dynamic>("dbo.spGetWithIdRules", new { Id = id }, connectionString);
            return results.FirstOrDefault();
        }

        public Task<IEnumerable<RuleModel>> GetRules(string sql, string connectionString) =>
            _dp.ReadData<RuleModel>(sql, connectionString);

        public async Task InsertRules(List<RuleModel> rules, string connectionString)
        {
            string parameterName = "rules";
            RuleCollection ruleCollection = new RuleCollection();
            
            foreach (var rule in rules)
            {
                ruleCollection.Add(rule);
            }
            await _db.InsertWithUDT("dbo.spInsertRules", parameterName, ruleCollection, connectionString);
        }

        public async Task UpdateRule(RuleModel rule, int id, string connectionString)
        {
            RuleModel oldRule = await GetRuleFromSP(id, connectionString);
            if (oldRule == null)
            {
                Exception error = new Exception($"RuleData: could not find rule with this id {id}");
                throw error;
            }
            else
            {
                string json = JsonConvert.SerializeObject(rule, Formatting.Indented);
                await _dp.SaveData("dbo.spUpdateRules", new { json = json }, connectionString);
            }
        }

        public async Task DeleteRule(int id, string connectionString)
        {
            string sql = "DELETE FROM pdo_qc_rules WHERE Id = @Id";
            await _dp.DeleteData(sql, new { id }, connectionString);
        }
    }
}
