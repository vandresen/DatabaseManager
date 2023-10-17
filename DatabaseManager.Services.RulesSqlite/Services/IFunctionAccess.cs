using DatabaseManager.Services.RulesSqlite.Models;

namespace DatabaseManager.Services.RulesSqlite.Services
{
    public interface IFunctionAccess
    {
        Task<IEnumerable<RuleFunctionsDto>> GetFunctions(string connectionString);
        Task<RuleFunctionsDto> GetFunction(int id, string connectionString);
        Task CreateUpdateFunction(RuleFunctionsDto function, string connectionString);
        Task DeleteFunction(int id, string connectionString);
    }
}
