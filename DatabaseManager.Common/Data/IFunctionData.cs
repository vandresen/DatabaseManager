using DatabaseManager.Shared;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DatabaseManager.Common.Data
{
    public interface IFunctionData
    {
        Task<RuleFunctions> GetFunctionFromSP(int id, string connectionString);
        Task<IEnumerable<RuleFunctions>> GetFunctions(string sql, string connectionString);
        Task<IEnumerable<RuleFunctions>> GetFunctionsFromSP(string connectionString);
        Task CreateFunction(RuleFunctions function, string connectionString);
        Task UpdateFunction(RuleFunctions function, string connectionString);
        Task DeleteFunction(int id, string connectionString);
    }
}
