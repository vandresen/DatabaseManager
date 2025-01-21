using AutoMapper;
using DatabaseManager.Services.Rules.Extensions;
using DatabaseManager.Services.Rules.Models;
using Newtonsoft.Json;
using System.Data;

namespace DatabaseManager.Services.Rules.Services
{
    public class RuleDBAccess : IRuleDBAccess
    {
        private readonly IDatabaseAccess _db;
        private readonly IMapper _mapper;

        public RuleDBAccess(IDatabaseAccess db, IMapper mapper)
        {
            _db = db;
            _mapper = mapper;
        }

        public async Task CreateUpdateRule(RuleModelDto rule, string connectionString)
        {
            RuleModel newRule = _mapper.Map<RuleModel>(rule);
            IEnumerable<RuleModelDto> rules = await _db.LoadData<RuleModelDto, dynamic>("dbo.spGetRules", new { }, connectionString);
            var ruleExist = rules.FirstOrDefault(m => m.RuleName == rule.RuleName);
            if (ruleExist == null)
            {
                var rulesWithLastKeyNumber = rules.Where(m => m.RuleType == rule.RuleType).
                    OrderByDescending(x => x.KeyNumber).FirstOrDefault();
                if (rulesWithLastKeyNumber == null) newRule.KeyNumber = 1;
                else newRule.KeyNumber = rulesWithLastKeyNumber.KeyNumber + 1;
                await InsertRule(newRule, connectionString);

            }
            else await UpdateRule(newRule, ruleExist, connectionString);
        }

        public async Task DeleteRule(int id, string connectionString)
        {
            string sql = "DELETE FROM pdo_qc_rules WHERE Id = @Id";
            await _db.DeleteData(sql, new { id }, connectionString);
        }

        public async Task<RuleModelDto> GetRule(int id, string connectionString)
        {
            var results = await _db.LoadData<RuleModelDto, dynamic>("dbo.spGetWithIdRules", new { Id = id }, connectionString);
            return results.FirstOrDefault();
        }

        public Task<IEnumerable<RuleModelDto>> GetRules(string connectionString) =>
            _db.LoadData<RuleModelDto, dynamic>("dbo.spGetRules", new { }, connectionString);

        private async Task InsertRule(RuleModel rule, string connectionString)
        {
            List<RuleModelDto> rules = new List<RuleModelDto>();
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

        private async Task UpdateRule(RuleModel rule, RuleModelDto oldRule, string connectionString)
        {
            rule.Id = oldRule.Id;
            string userName = await GetUserName(connectionString);
            rule.ModifiedBy = userName;
            string json = JsonConvert.SerializeObject(rule, Formatting.Indented);
            json = json.SetJsonDataObjectDate("ModifiedDate");
            await _db.SaveData("dbo.spUpdateRules", new { json = json }, connectionString);
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
