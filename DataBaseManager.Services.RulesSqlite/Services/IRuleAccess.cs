using DatabaseManager.Services.RulesSqlite.Models;

namespace DatabaseManager.Services.RulesSqlite.Services
{
    public interface IRuleAccess
    {
        Task CreateDatabaseRules();
        Task InitializeStandardRules();
        Task CreateUpdateRule(RuleModelDto rule, string connectionString);
    }
}
