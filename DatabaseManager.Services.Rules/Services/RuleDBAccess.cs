using Dapper;
using DatabaseManager.Services.Rules.Models;
using Microsoft.Data.SqlClient;
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
    public class RuleDBAccess : IRuleDBAccess
    {
        private readonly IDatabaseAccess _db;

        public RuleDBAccess(IDatabaseAccess db)
        {
            _db = db;
        }

        public async Task CreateUpdateRule(RuleModel rule, string connectionString)
        {
            IEnumerable<RuleModelDto> rules = await _db.LoadData<RuleModelDto, dynamic>("dbo.spGetRules", new { }, connectionString);
            var ruleExist = rules.FirstOrDefault(m => m.RuleName == rule.RuleName);
            if (ruleExist == null)
            {
                var rulesWithLastKeyNumber = rules.Where(m => m.RuleType == rule.RuleType).
                    OrderByDescending(x => x.KeyNumber).FirstOrDefault();
                if (rulesWithLastKeyNumber == null) rule.KeyNumber = 1;
                else rule.KeyNumber = rulesWithLastKeyNumber.KeyNumber + 1;
                await InsertRule(rule, connectionString);

            }
            else UpdateRule(rule, connectionString);
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

        private async Task InsertRule(RuleModel rule, string connectionString)
        {
            List<RuleModel> rules = new List<RuleModel>();
            string userName = await GetUserName(connectionString);
            rule.ModifiedBy = userName;
            rule.CreatedBy = userName;
            rule.RuleKey = GetRuleKey(rule);
            rule.CreatedDate = DateTime.Now;
            rule.ModifiedDate = DateTime.Now;
            string parameterName = "rules";
            RuleCollection ruleCollection = new RuleCollection
            {
                rule
            };
            _db.InsertWithUDT("dbo.spInsertRules", parameterName, ruleCollection, connectionString);
        }

        private string GetRuleKey(RuleModel rule)
        {
            string ruleKey = "";
            string strKey = rule.KeyNumber.ToString();
            RuleTypeDictionary rt = new RuleTypeDictionary();
            ruleKey = rt[rule.RuleType] + strKey;
            return ruleKey;
        }

        private void UpdateRule(RuleModel rule, string connectionString)
        {
            //ConnectParameters connector = await Common.GetConnectParameters(azureConnectionString, name);
            //if (connector.SourceType != "DataBase")
            //{
            //    Exception error = new Exception($"RuleManagement: data source must be a Database type");
            //    throw error;
            //}
            //RuleModel oldRule = await _ruleData.GetRuleFromSP(id, connector.ConnectionString);
            //if (oldRule == null)
            //{
            //    Exception error = new Exception($"RuleManagement: could not find rule");
            //    throw error;
            //}
            //else
            //{
            //    rule.Id = id;
            //    string userName = await _systemData.GetUserName(connector.ConnectionString);
            //    rule.ModifiedBy = userName;
            //    string json = JsonConvert.SerializeObject(rule, Formatting.Indented);
            //    json = Common.SetJsonDataObjectDate(json, "ModifiedDate");
            //    using (IDbConnection cnn = new SqlConnection(connector.ConnectionString))
            //    {
            //        var p = new DynamicParameters();
            //        p.Add("@json", json);
            //        string sql = "dbo.spUpdateRules";
            //        int recordsAffected = cnn.Execute(sql, p, commandType: CommandType.StoredProcedure);
            //    }
            //}
        }

        private async Task<string> GetUserName(string connectionString)
        {
            string sql = @"select stuff(suser_sname(), 1, charindex('\', suser_sname()), '') as UserName";
            IEnumerable<string> result = await _db.ReadData<string>(sql, connectionString);
            string userName = result.FirstOrDefault();
            if (userName == null) userName = "UNKNOWN";
            return userName;
        }
    }
}
