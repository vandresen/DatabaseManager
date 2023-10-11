using AutoMapper;
using DatabaseManager.Services.RulesSqlite.Models;
using Microsoft.Data.Sqlite;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using System.Threading.Tasks;

namespace DatabaseManager.Services.RulesSqlite.Services
{
    public class RuleAccess : IRuleAccess
    {
        private readonly string _databaseFile = @".\mydatabase.db";
        private readonly IDataAccess _id;
        private readonly ILogger<RuleAccess> _log;
        private readonly IFunctionAccess _fa;
        private readonly IMapper _mapper;
        private string _connectionString;
        private readonly IFileStorage _embeddedStorage;
        private string _getSql;
        private string _table = "pdo_qc_rules";
        private string _selectAttributes = "Id, DataType, RuleType, RuleParameters, RuleKey, RuleName, RuleFunction, DataAttribute, RuleFilter, FailRule, PredictionOrder, KeyNumber, Active, RuleDescription, CreatedBy, ModifiedBy, CreatedDate, ModifiedDate";

        public RuleAccess(IDataAccess id, ILogger<RuleAccess> log, IFunctionAccess fa,
            IMapper mapper)
        {
            _embeddedStorage = new EmbeddedFileStorage();
            _connectionString = @"Data Source=" + _databaseFile;
            _id = id;
            _log = log;
            _fa = fa;
            _mapper = mapper;
            _getSql = "Select " + _selectAttributes + " From " + _table;
        }
        public async Task CreateDatabaseRules()
        {
            if (!System.IO.File.Exists(_databaseFile))
            {
                using (SqliteConnection connection = new SqliteConnection($"Data Source={_databaseFile}"))
                {
                    connection.Open();
                    using (SqliteCommand command = connection.CreateCommand())
                    {
                        command.CommandText = "CREATE TABLE Dummy (Id INTEGER PRIMARY KEY)";
                        command.ExecuteNonQuery();
                    }
                }
            }

            string dropSql = "DROP TABLE IF EXISTS ";
            string createSql = "CREATE TABLE ";
            string sql = dropSql + "pdo_qc_rules;";
            sql = sql + createSql + "pdo_qc_rules" +
                "(" +
                "Id INTEGER PRIMARY KEY AUTOINCREMENT," +
                "Active TEXT," +
                "DataType TEXT NOT NULL," +
                "DataAttribute TEXT," +
                "RuleType TEXT NOT NULL," +
                "RuleName TEXT NOT NULL," +
                "RuleDescription TEXT NULL," +
                "RuleFunction TEXT NULL," +
                "RuleKey TEXT NULL," +
                "RuleParameters TEXT NULL," +
                "RuleFilter TEXT NULL," +
                "FailRule TEXT NULL," +
                "PredictionOrder INTEGER NULL," +
                "CreatedBy TEXT NULL," +
                "CreatedDate DATETIME NULL," +
                "ModifiedBy TEXT NULL," +
                "ModifiedDate DATETIME NULL," +
                "KeyNumber INTEGER NOT NULL" +
                ");";
            await _id.ExecuteSQL(sql, _connectionString);

            sql = dropSql + "pdo_rule_functions;";
            sql = sql + createSql + "pdo_rule_functions" +
                "(" +
                "Id INTEGER PRIMARY KEY AUTOINCREMENT," +
                "FunctionName TEXT NOT NULL," +
                "FunctionUrl TEXT NOT NULL," +
                "FunctionType TEXT," +
                "FunctionKey TEXT" +
                ");";
            await _id.ExecuteSQL(sql, _connectionString);
        }

        public async Task InitializeStandardRules()
        {
            await InitializeInternalRuleFunctions();
            string fileName = "StandardRules.json";
            string ruleString = await _embeddedStorage.ReadFile("", fileName);
            await SaveRulesToDatabase(ruleString);
        }

        private async Task SaveRulesToDatabase(string ruleString)
        {
            _log.LogInformation($"Save rules to database");
            List<RuleFunctionsDto> ruleFunctions = new List<RuleFunctionsDto>();
            IEnumerable<RuleFunctionsDto> functions = await _fa.GetFunctions(_connectionString);
            List<RuleModelDto> rules = JsonConvert.DeserializeObject<List<RuleModelDto>>(ruleString);
            foreach (var rule in rules)
            {
                string functionType = "";
                if (rule.RuleType == "Validity") functionType = "V";
                if (rule.RuleType == "Predictions") functionType = "P";
                RuleFunctionsDto ruleFunction = BuildFunctionData(rule.RuleFunction, functionType);
                RuleFunctionsDto functionIsInDB = functions.FirstOrDefault(s => s.FunctionName == ruleFunction.FunctionName);
                if (functionIsInDB == null)
                {
                    RuleFunctionsDto result = ruleFunctions.FirstOrDefault(s => s.FunctionName == ruleFunction.FunctionName);
                    if (result == null) ruleFunctions.Add(ruleFunction);
                }
                rule.RuleFunction = ruleFunction.FunctionName;
                await CreateUpdateRule(rule, _connectionString);
            }
            foreach (var function in ruleFunctions)
            {
                //await InsertFunction(function, connectionString);
            }
        }

        private RuleFunctionsDto BuildFunctionData(string ruleFunction, string functionType)
        {
            RuleFunctionsDto rf = new RuleFunctionsDto();
            int startFunctionName = ruleFunction.IndexOf(@"/api/");
            if (startFunctionName == -1) startFunctionName = 0;
            else startFunctionName = startFunctionName + 5;

            int endFunctionName = ruleFunction.IndexOf(@"?");
            string functionKey = "";
            if (endFunctionName == -1)
            {
                endFunctionName = ruleFunction.Length;
            }
            else
            {
                functionKey = ruleFunction.Substring((endFunctionName + 6));
            }

            int functionNameLength = endFunctionName - startFunctionName;
            string functionname = ruleFunction.Substring(startFunctionName, functionNameLength);
            string functionUrl = ruleFunction.Substring(0, endFunctionName);

            rf.FunctionName = functionname;
            rf.FunctionUrl = functionUrl;
            rf.FunctionKey = functionKey;
            rf.FunctionType = functionType;
            return rf;
        }

        private async Task InitializeInternalRuleFunctions()
        {
            string sql = "DELETE from pdo_rule_functions;";
            await _id.ExecuteSQL(sql, _connectionString);
            string baseSql = @"INSERT INTO pdo_rule_functions";
            sql = baseSql + @"(FunctionName, FunctionUrl) VALUES('Completeness', 'Completeness');";
            sql = sql + baseSql + "(FunctionName, FunctionUrl, FunctionType) VALUES('DeleteDataObject', 'DeleteDataObject', 'P');";
            sql = sql + baseSql + "(FunctionName, FunctionUrl) VALUES('Uniqueness', 'Uniqueness');";
            sql = sql + baseSql + "(FunctionName, FunctionUrl) VALUES('Entirety', 'Entirety');";
            sql = sql + baseSql + "(FunctionName, FunctionUrl) VALUES('Consistency', 'Consistency');";
            sql = sql + baseSql + "(FunctionName, FunctionUrl, FunctionType) VALUES('ValidityRange', 'ValidityRange', 'V');";
            sql = sql + baseSql + "(FunctionName, FunctionUrl, FunctionType) VALUES('CurveSpikes', 'CurveSpikes', 'V');";
            sql = sql + baseSql + "(FunctionName, FunctionUrl, FunctionType) VALUES('IsNumber', 'IsNumber', 'V');";
            sql = sql + baseSql + "(FunctionName, FunctionUrl, FunctionType) VALUES('StringLength', 'StringLength', 'V');";
            sql = sql + baseSql + "(FunctionName, FunctionUrl, FunctionType) VALUES('IsEqualTo', 'IsEqualTo', 'V');";
            sql = sql + baseSql + "(FunctionName, FunctionUrl, FunctionType) VALUES('IsGreaterThan', 'IsGreaterThan', 'V');";
            sql = sql + baseSql + "(FunctionName, FunctionUrl, FunctionType) VALUES('PredictDepthUsingIDW', 'PredictDepthUsingIDW', 'P');";
            sql = sql + baseSql + "(FunctionName, FunctionUrl, FunctionType) VALUES('PredictDominantLithology', 'PredictDominantLithology', 'P');";
            sql = sql + baseSql + "(FunctionName, FunctionUrl, FunctionType) VALUES('PredictFormationOrder', 'PredictFormationOrder', 'P');";
            sql = sql + baseSql + "(FunctionName, FunctionUrl, FunctionType) VALUES('PredictLogDepthAttributes', 'PredictLogDepthAttributes', 'P');";
            sql = sql + baseSql + "(FunctionName, FunctionUrl, FunctionType) VALUES('PredictMissingDataObjects', 'PredictMissingDataObjects', 'P');";
            await _id.ExecuteSQL(sql, _connectionString);
        }

        public async Task CreateUpdateRule(RuleModelDto rule, string connectionString)
        {
            RuleModel newRule = _mapper.Map<RuleModel>(rule);
            IEnumerable<RuleModelDto> rules = await _id.ReadData<RuleModelDto>(_getSql, connectionString);
            var ruleExist = rules.FirstOrDefault(m => m.RuleName == rule.RuleName);
            if (ruleExist == null)
            {
                var rulesWithLastKeyNumber = rules.Where(m => m.RuleType == rule.RuleType).
                    OrderByDescending(x => x.KeyNumber).FirstOrDefault();
                if (rulesWithLastKeyNumber == null) newRule.KeyNumber = 1;
                else newRule.KeyNumber = rulesWithLastKeyNumber.KeyNumber + 1;
                await InsertRule(newRule, connectionString);

            }
            //else await UpdateRule(newRule, ruleExist, connectionString);
        }

        private async Task InsertRule(RuleModel rule, string connectionString)
        {
            List<RuleModelDto> rules = new List<RuleModelDto>();
            rule.RuleKey = GetRuleKey(rule);
            rule.CreatedDate = DateTime.Now;
            rule.ModifiedDate = DateTime.Now;
            string sql = $"INSERT INTO {_table} " +
                "(DataType, RuleType, RuleParameters, RuleKey, RuleName, RuleFunction, DataAttribute, RuleFilter, FailRule, PredictionOrder, KeyNumber, Active, RuleDescription, CreatedDate, ModifiedDate) " +
                "VALUES(@DataType, @RuleType, @RuleParameters, @RuleKey, @RuleName, @RuleFunction, @DataAttribute, @RuleFilter, @FailRule, @PredictionOrder, @KeyNumber, @Active, @RuleDescription, @CreatedDate, @ModifiedDate)";
            await _id.InsertUpdateData(sql, rule, connectionString);
            //string parameterName = "rules";
            //RuleCollection ruleCollection = new RuleCollection
            //{
            //    rule
            //};
            //_db.InsertWithUDT("dbo.spInsertRules", parameterName, ruleCollection, connectionString);
        }

        private string GetRuleKey(RuleModel rule)
        {
            string ruleKey = "";
            string strKey = rule.KeyNumber.ToString();
            RuleTypeDictionary rt = new RuleTypeDictionary();
            ruleKey = rt[rule.RuleType] + strKey;
            return ruleKey;
        }
    }
}
