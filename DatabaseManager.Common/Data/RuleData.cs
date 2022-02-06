using DatabaseManager.Common.DBAccess;
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

        public RuleData(IDapperDataAccess dp)
        {
            _dp = dp;
        }

        public Task<IEnumerable<RuleModel>> GetRulesFromSP(string connectionString) =>
            _dp.LoadData<RuleModel, dynamic>("dbo.spRule_GetAll", new { }, connectionString);

        public async Task<RuleModel?> GetRuleFromSP(int id, string connectionString)
        {
            var results = await _dp.LoadData<RuleModel, dynamic>("dbo.spRule_Get", new { Id = id }, connectionString);
            return results.FirstOrDefault();
        }

        public Task<IEnumerable<RuleModel>> GetRules(string sql, string connectionString) =>
            _dp.ReadData<RuleModel>(sql, connectionString);

    }
}
