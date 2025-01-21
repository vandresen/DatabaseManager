using DatabaseManager.Common.DBAccess;
using DatabaseManager.Shared;
using Newtonsoft.Json;

namespace DatabaseManager.Common.Data
{
    public class FunctionData : IFunctionData
    {
        private readonly IDapperDataAccess _dp;

        public FunctionData(IDapperDataAccess dp)
        {
            _dp = dp;
        }

        public async Task CreateFunction(RuleFunctions function, string connectionString)
        {
            string json = JsonConvert.SerializeObject(function, Formatting.Indented);
            await _dp.SaveData("dbo.spInsertFunctions", new { json = json }, connectionString);
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

        public async Task UpdateFunction(RuleFunctions function, string connectionString)
        {
            string json = JsonConvert.SerializeObject(function, Formatting.Indented);
            await _dp.SaveData("dbo.spUpdateFunctions", new { json = json }, connectionString);
        }

        public async Task DeleteFunction(int id, string connectionString)
        {
            string sql = "DELETE FROM pdo_rule_functions WHERE Id = @Id";
            await _dp.DeleteData(sql, new { id }, connectionString);
        }
    }
}
