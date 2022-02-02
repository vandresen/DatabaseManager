using DatabaseManager.Common.DBAccess;
using DatabaseManager.Shared;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DatabaseManager.Common.Data
{
    public class RuleData
    {
        private readonly IDapperDataAccess _dp;

        public RuleData(IDapperDataAccess dp)
        {
            _dp = dp;
        }

        public Task<IEnumerable<RuleModel>> GetRulesFromSP(string connectionString) =>
            _dp.LoadData<RuleModel, dynamic>("dbo.spRules_GetAll", new { }, connectionString);
    }
}
