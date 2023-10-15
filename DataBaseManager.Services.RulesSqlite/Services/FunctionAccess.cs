using AutoMapper;
using DatabaseManager.Services.RulesSqlite.Models;
using Newtonsoft.Json;

namespace DatabaseManager.Services.RulesSqlite.Services
{
    public class FunctionAccess : IFunctionAccess
    {
        private readonly IDataAccess _id;
        private string _getSql;
        private string _table = "pdo_rule_functions";
        private string _selectAttributes = "Id, FunctionName, FunctionUrl, FunctionKey, FunctionType";
        private readonly IMapper _mapper;

        public FunctionAccess(IDataAccess id, IMapper mapper)
        {
            _id = id;
            _getSql = "Select " + _selectAttributes + " From " + _table;
            _mapper = mapper;
        }

        public async Task CreateUpdateFunction(RuleFunctionsDto function, string connectionString)
        {
            RuleFunctions newFunction = _mapper.Map<RuleFunctions>(function);
            IEnumerable<RuleFunctionsDto> existingFunctions = await _id.ReadData<RuleFunctionsDto>(_getSql, connectionString);
            var functionExist = existingFunctions.FirstOrDefault(m => m.FunctionName == function.FunctionName);
            if (functionExist == null)
            {
                //string json = JsonConvert.SerializeObject(newFunction, Formatting.Indented);
                //await _db.SaveData("dbo.spInsertFunctions", new { json = json }, connectionString);

            }
            else
            {
                //newFunction.Id = functionExist.Id;
                //string json = JsonConvert.SerializeObject(newFunction, Formatting.Indented);
                //await _db.SaveData("dbo.spUpdateFunctions", new { json = json }, connectionString);
            }
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
