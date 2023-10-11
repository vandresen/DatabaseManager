using DatabaseManager.Services.RulesSqlite.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DatabaseManager.Services.RulesSqlite.Services
{
    public class FunctionAccess : IFunctionAccess
    {
        private readonly IDataAccess _id;
        private string _getSql;
        private string _table = "pdo_rule_functions";
        private string _selectAttributes = "Id, FunctionName, FunctionUrl, FunctionKey, FunctionType";

        public FunctionAccess(IDataAccess id)
        {
            _id = id;
            _getSql = "Select " + _selectAttributes + " From " + _table;
        }

        public Task CreateUpdateFunction(RuleFunctionsDto function, string connectionString)
        {
            throw new NotImplementedException();
        }

        public Task DeleteFunction(int id, string connectionString)
        {
            throw new NotImplementedException();
        }

        public Task<RuleFunctionsDto> GetFunction(int id, string connectionString)
        {
            throw new NotImplementedException();
        }

        public async Task<IEnumerable<RuleFunctionsDto>> GetFunctions(string connectionString)
        {
            IEnumerable<RuleFunctionsDto> result = await _id.ReadData<RuleFunctionsDto>(_getSql, connectionString);
            return result;
        }
    }
}
