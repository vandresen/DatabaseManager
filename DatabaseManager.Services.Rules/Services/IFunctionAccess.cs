using DatabaseManager.Services.Rules.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DatabaseManager.Services.Rules.Services
{
    public interface IFunctionAccess
    {
        Task<IEnumerable<RuleFunctionsDto>> GetFunctions(string connectionString);
        Task<RuleFunctionsDto> GetFunction(int id, string connectionString);
        Task CreateUpdateFunction(RuleFunctionsDto function, string connectionString);
        Task DeleteFunction(int id, string connectionString);
    }
}
