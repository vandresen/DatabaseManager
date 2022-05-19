using DatabaseManager.Common.DBAccess;
using DatabaseManager.Shared;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DatabaseManager.Common.Data
{
    public class FunctionData : IFunctionData
    {
        private readonly IDapperDataAccess _dp;

        public FunctionData(IDapperDataAccess dp)
        {
            _dp = dp;
        }

        public async Task<RuleFunctions> GetFunctionFromSP(int id, string connectionString)
        {
            var results = await _dp.LoadData<RuleFunctions, dynamic>("dbo.spGetWithIdFunctions", new { Id = id }, connectionString);
            return results.FirstOrDefault();
        }

        public Task<IEnumerable<RuleFunctions>> GetFunctions(string sql, string connectionString)
        {
            throw new NotImplementedException();
        }

        public Task<IEnumerable<RuleFunctions>> GetFunctionsFromSP(string connectionString) =>
            _dp.LoadData<RuleFunctions, dynamic>("dbo.spGetFunctions", new { }, connectionString);
    }
}
