using DatabaseManager.Services.Rules.Extensions;
using DatabaseManager.Services.Rules.Models;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Runtime.Intrinsics.Arm;
using System.Text;
using System.Threading.Tasks;

namespace DatabaseManager.Services.Rules.Services
{
    public class FunctionAccess : IFunctionAccess
    {
        private readonly IDatabaseAccess _db;

        public FunctionAccess(IDatabaseAccess db)
        {
            _db = db;
        }

        public async Task CreateUpdateFunction(RuleFunctionsDto function, string connectionString)
        {
            IEnumerable<RuleFunctionsDto> existingFunctions = await _db.LoadData<RuleFunctionsDto, dynamic>("dbo.spGetFunctions", new { }, connectionString);
            var functionExist = existingFunctions.FirstOrDefault(m => m.FunctionName == function.FunctionName);
            if (functionExist == null)
            {
                string json = JsonConvert.SerializeObject(function, Formatting.Indented);
                await _db.SaveData("dbo.spInsertFunctions", new { json = json }, connectionString);

            }
            else 
            {
                function.Id = functionExist.Id;
                string json = JsonConvert.SerializeObject(function, Formatting.Indented);
                await _db.SaveData("dbo.spUpdateFunctions", new { json = json }, connectionString);
            }
        }

        public async Task DeleteFunction(int id, string connectionString)
        {
            string sql = "DELETE FROM pdo_rule_functions WHERE Id = @Id";
            await _db.DeleteData(sql, new { id }, connectionString);
        }

        public async Task<RuleFunctionsDto> GetFunction(int id, string connectionString)
        {
            var results = await _db.LoadData<RuleFunctionsDto, dynamic>("dbo.spGetWithIdFunctions", new { Id = id }, connectionString);
            return results.FirstOrDefault();
        }

        public Task<IEnumerable<RuleFunctionsDto>> GetFunctions(string connectionString) =>
            _db.LoadData<RuleFunctionsDto, dynamic>("dbo.spGetFunctions", new { }, connectionString);
    }
}
