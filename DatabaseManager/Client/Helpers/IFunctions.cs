using DatabaseManager.Shared;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace DatabaseManager.Client.Helpers
{
    public interface IFunctions
    {
        Task<List<RuleFunctions>> GetFunctions(string source);
        Task InsertFunction(RuleFunctions function, string source);
    }
}
