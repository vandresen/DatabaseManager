using Dapper;
using DatabaseManager.Common.Data;
using DatabaseManager.Common.DBAccess;
using DatabaseManager.Common.Entities;
using DatabaseManager.Common.Services;
using DatabaseManager.Shared;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json;
using System.Data;

namespace DatabaseManager.Common.Helpers
{
    public class RuleManagement
    {
        private readonly string azureConnectionString;
        private readonly IFileStorageServiceCommon _fileStorage;
        private readonly IAzureDataAccess _azureDataTables;
        private readonly IRuleData _ruleData;
        private readonly ISystemData _systemData;
        private readonly IFunctionData _functionData;
        private readonly IPredictionSetData _predictionSetData;
        private readonly IDapperDataAccess _dp;
        private readonly IADODataAccess _db;
        private readonly string container = "sources";
        private readonly string predictionContainer = "predictions";
        private readonly string ruleShare = "rules";
        private string getSql = "Select Id, DataType, RuleType, RuleParameters, " +
            "RuleKey, RuleName, RuleFunction, DataAttribute, RuleFilter, FailRule, " +
            "PredictionOrder, KeyNumber, Active, RuleDescription, CreatedBy, ModifiedBy, " +
            "CreatedDate, ModifiedDate from pdo_qc_rules";
        private string functionSql = "Select Id, FunctionName, FunctionUrl, FunctionKey, " +
            "FunctionType from pdo_rule_functions";

        public RuleManagement()
        {
        }

        public RuleManagement(string azureConnectionString)
        {
            this.azureConnectionString = azureConnectionString;
            var builder = new ConfigurationBuilder();
            IConfiguration configuration = builder.Build();
            _fileStorage = new AzureFileStorageServiceCommon(configuration);
            _fileStorage.SetConnectionString(azureConnectionString);
            _azureDataTables = new AzureDataTable(configuration);
            _azureDataTables.SetConnectionString(azureConnectionString);
            _predictionSetData = new PredictionSetData(_azureDataTables);
            _dp = new DapperDataAccess();
            _db = new ADODataAccess();
            _ruleData = new RuleData(_dp, _db);
            _systemData = new SystemDBData(_dp);
            _functionData = new FunctionData(_dp);
        }

        public DataAccessDef GetDataAccessDefinition(string dataType)
        {
            DataAccessDef dataAccessDef = new DataAccessDef();

            if (dataType == "Rules")
            {
                dataAccessDef.DataType = "Rules";
                dataAccessDef.Select = getSql;
                dataAccessDef.Keys = "Id";
            }
            else if (dataType == "Functions")
            {
                dataAccessDef.DataType = "Functions";
                dataAccessDef.Select = functionSql;
                dataAccessDef.Keys = "Id";
            }
            else
            {
                throw new InvalidOperationException("Not a valif data type");
            }

            return dataAccessDef;
        }

        public async Task<string> GetRules(string sourceName)
        {
            string result = "";
            ConnectParameters connector = await Common.GetConnectParameters(azureConnectionString, sourceName);
            IEnumerable<RuleModel> rules = await _ruleData.GetRulesFromSP(connector.ConnectionString);
            if (rules.Any())result = JsonConvert.SerializeObject(rules, Formatting.Indented);
            return result;
        }

        public async Task<string> GetRule(string sourceName, int id)
        {
            string result = "";
            ConnectParameters connector = await Common.GetConnectParameters(azureConnectionString, sourceName);
            RuleModel rule = await _ruleData.GetRuleFromSP(id, connector.ConnectionString);
            result = JsonConvert.SerializeObject(rule, Formatting.Indented);
            return result;
        }

        public async Task<string> GetFunctions(string sourceName)
        {
            string result = "";
            ConnectParameters connector = await Common.GetConnectParameters(azureConnectionString, sourceName);
            IEnumerable<RuleFunctions> functions = await _functionData.GetFunctionsFromSP(connector.ConnectionString);
            if (functions.Any()) result = JsonConvert.SerializeObject(functions, Formatting.Indented);
            return result;
        }

        public async Task<string> GetFunction(string sourceName, int id)
        {
            string result = "";
            ConnectParameters connector = await Common.GetConnectParameters(azureConnectionString, sourceName);
            RuleFunctions function = await _functionData.GetFunctionFromSP(id, connector.ConnectionString);
            result = JsonConvert.SerializeObject(function, Formatting.Indented);
            return result;
        }

        public async Task<string> GetActiveRules(string sourceName)
        {
            string result = "";
            ConnectParameters connector = await Common.GetConnectParameters(azureConnectionString, sourceName);
            IEnumerable<RuleModel> rules = await _ruleData.GetRulesFromSP(connector.ConnectionString);
            List<RuleModel> activeRules = rules.Where(x => x.Active == "Y").ToList();
            if (activeRules != null) result = JsonConvert.SerializeObject(activeRules, Formatting.Indented);
            return result;
        }

        public async Task<string> GetActiveQCRules(string sourceName)
        {
            string result = "";
            ConnectParameters connector = await Common.GetConnectParameters(azureConnectionString, sourceName);
            IEnumerable<RuleModel> rules = await _ruleData.GetRulesFromSP(connector.ConnectionString);
            List<RuleModel> activeRules = rules.
                Where(x => x.Active == "Y" && x.RuleType != "Predictions").
                ToList();
            if (activeRules != null) result = JsonConvert.SerializeObject(activeRules, Formatting.Indented);
            return result;
        }

        public async Task<string> GetActivePredictionRules(string sourceName)
        {
            string result = "";
            ConnectParameters connector = await Common.GetConnectParameters(azureConnectionString, sourceName);
            IEnumerable<RuleModel> rules = await _ruleData.GetRulesFromSP(connector.ConnectionString);
            List<RuleModel> activeRules = rules.
                Where(x => x.Active == "Y" && x.RuleType == "Predictions").
                OrderBy(x => x.PredictionOrder).
                ToList();
            if (activeRules != null) result = JsonConvert.SerializeObject(activeRules, Formatting.Indented);
            return result;
        }

        public async Task<RuleModel> GetRuleAndFunction(string sourceName, int id)
        {
            ConnectParameters connector = await Common.GetConnectParameters(azureConnectionString, sourceName);
            RuleModel rule = await _ruleData.GetRuleFromSP(id, connector.ConnectionString);
            IEnumerable<RuleFunctions> functions = await _functionData.GetFunctionsFromSP(connector.ConnectionString);
            RuleFunctions function = functions.FirstOrDefault(x => x.FunctionName == rule.RuleFunction);
            if (function != null)
            {
                if (!string.IsNullOrEmpty(function.FunctionKey)) 
                    rule.RuleFunction = function.FunctionUrl + "?code=" + function.FunctionKey;
            }
            return rule;
        }

        public async Task<string> GetPredictions()
        {
            string result = "";
            List<PredictionSet> predictionSets = _predictionSetData.GetPredictionDataSets();
            result = JsonConvert.SerializeObject(predictionSets, Formatting.Indented);
            return result;
        }

        public async Task<string> GetPrediction(string name)
        {
            string result = "";
            string ruleName = name + ".json";
            result = await _fileStorage.ReadFile(ruleShare, ruleName);
            if (string.IsNullOrEmpty(result))
            {
                Exception error = new Exception($"Empty data from {ruleName}");
                throw error;
            }
            return result;
        }

        public async Task<string> GetRuleInfo()
        {
            string result = "";
            string accessJson = await _fileStorage.ReadFile("connectdefinition", "PPDMDataAccess.json");
            List<DataAccessDef> accessDefs = JsonConvert.DeserializeObject<List<DataAccessDef>>(accessJson);
            RuleInfo ruleInfo = new RuleInfo();
            ruleInfo.DataTypeOptions = new List<string>();
            ruleInfo.DataAttributes = new Dictionary<string, string>();
            foreach (DataAccessDef accessDef in accessDefs)
            {
                ruleInfo.DataTypeOptions.Add(accessDef.DataType);
                string[] attributeArray = Helpers.Common.GetAttributes(accessDef.Select);
                string attributes = String.Join(",", attributeArray);
                ruleInfo.DataAttributes.Add(accessDef.DataType, attributes);
            }
            result = JsonConvert.SerializeObject(ruleInfo, Formatting.Indented);
            return result;
        }

        public async Task SaveRule(string sourceName, RuleModel rule)
        {
            ConnectParameters connector = await Common.GetConnectParameters(azureConnectionString, sourceName);
            IEnumerable<RuleModel> rules = await _ruleData.GetRulesFromSP(connector.ConnectionString);
            var ruleExist = rules.FirstOrDefault(m => m.RuleName == rule.RuleName);
            if (ruleExist == null)
            {
                var rulesWithLastKeyNumber = rules.Where(m => m.RuleType == rule.RuleType).
                    OrderByDescending(x => x.KeyNumber).FirstOrDefault();
                if (rulesWithLastKeyNumber == null) rule.KeyNumber = 1;
                else rule.KeyNumber = rulesWithLastKeyNumber.KeyNumber + 1;
                await InsertRule(rule, connector.ConnectionString);
            }
            else
            {
                Exception error = new Exception($"RuleManagement: rule already exist");
                throw error;
            }
        }

        public async Task SaveFunction(string sourceName, RuleFunctions function)
        {
            ConnectParameters connector = await Common.GetConnectParameters(azureConnectionString, sourceName);

            IEnumerable<RuleFunctions> existingFunctions = await _functionData.GetFunctionsFromSP(connector.ConnectionString);
            var functionExist = existingFunctions.FirstOrDefault(m => m.FunctionName == function.FunctionName);

            if (functionExist == null)
            {
                await _functionData.CreateFunction(function, connector.ConnectionString);
                
            }
            else
            {
                Exception error = new Exception($"RuleManagement: could not find function");
                throw error;
            }
        }

        public async Task SavePredictionSet(string name, PredictionSet set)
        {
            
            List<RuleModel> rules = set.RuleSet;
            List<PredictionSet> tmpPredictionSets = _predictionSetData.GetPredictionDataSets();
            if(tmpPredictionSets.FirstOrDefault(c => c.Name == set.Name) != null)
            {
                Exception error = new Exception($"RuleManagement: prediction set already exist");
                throw error;
            }
            
            string fileName = name + ".json";
            string json = JsonConvert.SerializeObject(rules, Formatting.Indented);
            string url = await _fileStorage.SaveFileUri(ruleShare, fileName, json);
            set.RuleUrl = url;
            _predictionSetData.SavePredictionDataSet(set);
        }

        public async Task SaveRulesToDatabase(string ruleString, ConnectParameters connector)
        {
            List<RuleFunctions> ruleFunctions = new List<RuleFunctions>();
            string query = "";
            IEnumerable<RuleFunctions> functions = await _functionData.GetFunctionsFromSP(connector.ConnectionString);
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
            await _ruleData.InsertRules(rules, connector.ConnectionString);
            foreach (var function in ruleFunctions)
            {
                await _functionData.CreateFunction(function, connector.ConnectionString);
            }
        }

        public async Task UpdateRule(string name, int id, RuleModel rule)
        {
            ConnectParameters connector = await Common.GetConnectParameters(azureConnectionString, name);
            if (connector.SourceType != "DataBase")
            {
                Exception error = new Exception($"RuleManagement: data source must be a Database type");
                throw error;
            }
            rule.Id = id;
            string userName = await _systemData.GetUserName(connector.ConnectionString);
            rule.ModifiedBy = userName;
            await _ruleData.UpdateRule(rule, id, connector.ConnectionString);
        }

        public async Task UpdateFunction(string sourceName, int id, RuleFunctions function)
        {
            ConnectParameters connector = await Common.GetConnectParameters(azureConnectionString, sourceName);
            if (connector.SourceType != "DataBase")
            {
                Exception error = new Exception($"RuleManagement: data source must be a Database type");
                throw error;
            }

            IEnumerable<RuleFunctions> existingFunctions = await _functionData.GetFunctionsFromSP(connector.ConnectionString);
            var functionExist = existingFunctions.FirstOrDefault(m => m.Id == function.Id);
            if (functionExist == null) 
            {
                Exception error = new Exception($"RuleManagement: could not find function");
                throw error;
            }
            else
            {
                await _functionData.UpdateFunction(function, connector.ConnectionString);
            }
        }

        public async Task DeleteRule(string name, int id)
        {
            ConnectParameters connector = await Common.GetConnectParameters(azureConnectionString, name);
            if (connector.SourceType != "DataBase")
            {
                Exception error = new Exception($"RuleManagement: data source must be a Database type");
                throw error;
            }
            await _ruleData.DeleteRule(id, connector.ConnectionString);
        }

        public async Task DeleteFunction(string source, int id)
        {
            ConnectParameters connector = await Common.GetConnectParameters(azureConnectionString, source);
            if (connector.SourceType != "DataBase")
            {
                Exception error = new Exception($"RuleManagement: data source must be a Database type");
                throw error;
            }
            await _functionData.DeleteFunction(id, connector.ConnectionString);
        }

        public async Task DeletePrediction(string name)
        {
            _predictionSetData.DeletePredictionDataSet(name);
            string ruleFile = name + ".json";
            await _fileStorage.DeleteFile(ruleShare, ruleFile);
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

        private async Task InsertRule(RuleModel rule, string connectionString)
        {
            List<RuleModel> rules = new List<RuleModel>();
            string userName = await _systemData.GetUserName(connectionString);
            rule.ModifiedBy = userName;
            rule.CreatedBy = userName;
            rule.RuleKey = GetRuleKey(rule, connectionString);
            rule.CreatedDate = DateTime.Now;
            rule.ModifiedDate = DateTime.Now;
            rules.Add(rule);
            await _ruleData.InsertRules(rules, connectionString);
        }

        private string GetRuleKey(RuleModel rule, string connectionString)
        {
            string ruleKey = "";
            string strKey = rule.KeyNumber.ToString();
            RuleTypeDictionary rt = new RuleTypeDictionary();
            ruleKey = rt[rule.RuleType] + strKey;
            return ruleKey;
        }
    }
}
