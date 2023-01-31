using DatabaseManager.Services.Rules.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Intrinsics.Arm;
using System.Text;
using System.Threading.Tasks;

namespace DatabaseManager.Services.Rules.Services
{
    public class RuleDBAccess : IRuleDBAccess
    {
        private readonly IDatabaseAccess _db;

        public RuleDBAccess(IDatabaseAccess db)
        {
            _db = db;
        }

        public Task<RuleModelDto> CreateUpdateRule(RuleModelDto connectParameters, string connectionString)
        {
            throw new NotImplementedException();
        }

        public Task<bool> DeleteRule(int id, string connectionString)
        {
            throw new NotImplementedException();
        }

        public Task<RuleModelDto> GetRule(int id, string connectionString)
        {
            throw new NotImplementedException();
        }

        public Task<IEnumerable<RuleModelDto>> GetRules(string connectionString) =>
            _db.LoadData<RuleModelDto, dynamic>("dbo.spGetRules", new { }, connectionString);
    }
}
