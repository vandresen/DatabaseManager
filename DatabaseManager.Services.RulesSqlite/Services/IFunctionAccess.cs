using DatabaseManager.Services.RulesSqlite.Models;

namespace DatabaseManager.Services.RulesSqlite.Services
{
    public interface IFunctionAccess
    {
        Task<IEnumerable<RuleFunctionsDto>> GetFunctions();
        Task<RuleFunctionsDto> GetFunction(int id);
        Task CreateUpdateFunction(RuleFunctionsDto function);
        Task DeleteFunction(int id);
    }
}
