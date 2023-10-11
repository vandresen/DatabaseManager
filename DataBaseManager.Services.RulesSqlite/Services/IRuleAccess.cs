using DatabaseManager.Services.RulesSqlite.Models;
using System.Threading.Tasks;

namespace DatabaseManager.Services.RulesSqlite.Services
{
    public interface IRuleAccess
    {
        Task CreateDatabaseRules();
        Task InitializeStandardRules();
        Task CreateUpdateRule(RuleModelDto rule, string connectionString);
    }
}
