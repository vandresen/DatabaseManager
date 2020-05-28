using DatabaseManager.Shared;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace DatabaseManager.Client.Helpers
{
    public interface IRules
    {
        Task DeleteRule(string source, int id);
        Task<RuleModel> GetRule(string source, int id);
        Task<RuleInfo> GetRuleInfo(string source);
        Task<List<RuleModel>> GetRules(string source);
        Task InsertRule(RuleModel rule, string source);
        Task UpdateRule(RuleModel rule, string source, int id);
    }
}
