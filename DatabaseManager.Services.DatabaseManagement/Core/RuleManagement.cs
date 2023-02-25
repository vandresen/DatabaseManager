using Dapper;
using DatabaseManager.Services.DatabaseManagement.Models;
using DatabaseManager.Services.DatabaseManagement.Services;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Reflection.Metadata;
using System.Text;
using System.Threading.Tasks;

namespace DatabaseManager.Services.DatabaseManagement.Core
{
    public class RuleManagement
    {
        private readonly string _azureConnectionString;
        private readonly ILogger _logger;
        private readonly IDatabaseAccessService _DbStorage;

        public RuleManagement(string azureConnectionString, ILogger logger)
        {
            _azureConnectionString = azureConnectionString;
            _logger = logger;
            _DbStorage = new SQLServerAccessService();
        }

        public async Task SaveRulesToDatabase(string ruleString, string connectionString)
        {
            _logger.LogInformation($"Save rules to database");
            List<RuleFunctions> ruleFunctions = new List<RuleFunctions>();
            IEnumerable<RuleFunctions> functions = await GetFunctions(connectionString);
            List<RuleModel> rules = JsonConvert.DeserializeObject<List<RuleModel>>(ruleString);
            foreach (var rule in rules)
            {
                string functionType = "";
                if (rule.RuleType == "Validity") functionType = "V";
                if (rule.RuleType == "Predictions") functionType = "P";
                RuleFunctions ruleFunction = BuildFunctionData(rule.RuleFunction, functionType);
                RuleFunctions functionIsInDB = functions.FirstOrDefault(s => s.FunctionName == ruleFunction.FunctionName);
                if (functionIsInDB == null)
                {
                    RuleFunctions result = ruleFunctions.FirstOrDefault(s => s.FunctionName == ruleFunction.FunctionName);
                    if (result == null) ruleFunctions.Add(ruleFunction);
                }
                rule.RuleFunction = ruleFunction.FunctionName;
            }
            await InsertRules(rules, connectionString);
            foreach (var function in ruleFunctions)
            {
                await InsertFunction(function, connectionString);
            }
        }

        private RuleFunctions BuildFunctionData(string ruleFunction, string functionType)
        {
            RuleFunctions rf = new RuleFunctions();
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

        private async Task InsertFunction(RuleFunctions function, string connectionString)
        {
            string json = JsonConvert.SerializeObject(function, Formatting.Indented);
            await _DbStorage.SaveData("dbo.spInsertFunctions", new {json = json }, connectionString);
        }

        private Task<IEnumerable<RuleFunctions>> GetFunctions(string connectionString) =>
            _DbStorage.LoadData<RuleFunctions, dynamic>("dbo.spGetFunctions", new { }, connectionString);

        public async Task InsertRules(List<RuleModel> rules, string connectionString)
        {
            string parameterName = "rules";
            RuleCollection ruleCollection = new RuleCollection();

            foreach (var rule in rules)
            {
                ruleCollection.Add(rule);
            }
            await _DbStorage.InsertWithUDT("dbo.spInsertRules", parameterName, ruleCollection, connectionString);
        }
    }
}
