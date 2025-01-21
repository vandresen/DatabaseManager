using DatabaseManager.Shared;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace DatabaseManager.Common.Data
{
    public interface IRuleData
    {
        Task<RuleModel> GetRuleFromSP(int id, string connectionString);
        Task<IEnumerable<RuleModel>> GetRules(string sql, string connectionString);
        Task<IEnumerable<RuleModel>> GetRulesFromSP(string connectionString);
        Task InsertRules(List<RuleModel> rules, string connectionString);
        Task UpdateRule(RuleModel rule, int id, string connectionString);
        Task DeleteRule(int id, string connectionString);
    }
}