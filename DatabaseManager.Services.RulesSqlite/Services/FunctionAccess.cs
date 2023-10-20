using AutoMapper;
using DatabaseManager.Services.RulesSqlite.Models;
using Newtonsoft.Json;

namespace DatabaseManager.Services.RulesSqlite.Services
{
    public class FunctionAccess : IFunctionAccess
    {
        private readonly string _databaseFile = @".\mydatabase.db";
        private string _connectionString;
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
            _connectionString = @"Data Source=" + _databaseFile;
        }

        public async Task CreateUpdateFunction(RuleFunctionsDto function)
        {
            RuleFunctions newFunction = _mapper.Map<RuleFunctions>(function);
            IEnumerable<RuleFunctionsDto> existingFunctions = await _id.ReadData<RuleFunctionsDto>(_getSql, _connectionString);
            var functionExist = existingFunctions.FirstOrDefault(m => m.FunctionName == function.FunctionName);
            if (functionExist == null)
            {
                await InsertFunction(newFunction);
            }
            else
            {
                await UpdateFunction(newFunction, function);
            }
        }

        public async Task DeleteFunction(int id)
        {
            string sql = $"DELETE FROM {_table} WHERE Id = {id}";
            await _id.ExecuteSQL(sql, _connectionString);
        }

        public async Task<RuleFunctionsDto> GetFunction(int id)
        {
            string sql = _getSql + $" WHERE Id = {id}";
            var results = await _id.ReadData<RuleFunctionsDto>(sql, _connectionString);
            return results.FirstOrDefault();
        }

        public async Task<IEnumerable<RuleFunctionsDto>> GetFunctions()
        {
            IEnumerable<RuleFunctionsDto> result = await _id.ReadData<RuleFunctionsDto>(_getSql, _connectionString);
            return result;
        }

        private async Task InsertFunction(RuleFunctions function)
        {
            string sql = $"INSERT INTO {_table} " +
                "(FunctionName, FunctionUrl, FunctionKey, FunctionType) " +
                "VALUES(@FunctionName, @FunctionUrl, @FunctionKey, @FunctionType)";
            await _id.InsertUpdateData(sql, function, _connectionString);
        }

        private async Task UpdateFunction(RuleFunctions function, RuleFunctionsDto oldfunction)
        {
            function.Id = oldfunction.Id;
            string sql = $"UPDATE {_table} SET " +
                "FunctionName=@FunctionName, FunctionUrl=@FunctionUrl, FunctionKey=@FunctionKey, FunctionType=@FunctionType " +
                $"WHERE Id = {function.Id}";
            await _id.InsertUpdateData(sql, function, _connectionString);
        }
    }
}
