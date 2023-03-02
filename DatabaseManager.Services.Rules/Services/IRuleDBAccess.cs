using DatabaseManager.Services.Rules.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DatabaseManager.Services.Rules.Services
{
    public interface IRuleDBAccess
    {
        Task<IEnumerable<RuleModelDto>> GetRules(string connectionString);
        Task<RuleModelDto> GetRule(int id, string connectionString);
        Task CreateUpdateRule(RuleModelDto rule, string connectionString);
        Task DeleteRule(int id, string connectionString);
    }
}
