using DatabaseManager.Shared;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace DatabaseManager.Client.Helpers
{
    public interface IFunctions
    {
        Task DeleteFunction(string source, int id);
        Task<RuleFunctions> GetFunction(string source, int id);
        Task<List<RuleFunctions>> GetFunctions(string source);
        Task InsertFunction(RuleFunctions function, string source);
        Task UpdateFunction(RuleFunctions function, string source, int id);
    }
}
