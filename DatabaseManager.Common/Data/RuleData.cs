using DatabaseManager.Common.DBAccess;
using DatabaseManager.Common.Entities;
using DatabaseManager.Shared;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

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
    }
}
