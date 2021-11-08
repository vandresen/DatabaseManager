using AutoMapper;
using Dapper;
using DatabaseManager.Common.Entities;
using DatabaseManager.Common.Services;
using DatabaseManager.Shared;
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DatabaseManager.Common.Helpers
{
    public class RuleManagement
    {
        private readonly string azureConnectionString;
        private readonly IFileStorageServiceCommon _fileStorage;
        private readonly ITableStorageServiceCommon _tableStorage;
        private DbUtilities _dbConn;
        private readonly string container = "sources";
        private readonly string predictionContainer = "predictions";
        private readonly string ruleShare = "rules";
        private IMapper _mapper;
        private string getSql = "Select Id, DataType, RuleType, RuleParameters, " +
            "RuleKey, RuleName, RuleFunction, DataAttribute, RuleFilter, FailRule, " +
            "PredictionOrder, KeyNumber, Active, RuleDescription, CreatedBy, ModifiedBy, " +
            "CreatedDate, ModifiedDate from pdo_qc_rules";
        private string functionSql = "Select Id, FunctionName, FunctionUrl, FunctionKey, " +
            "FunctionType from pdo_rule_functions";

        public RuleManagement(string azureConnectionString)
        {
            this.azureConnectionString = azureConnectionString;
            var builder = new ConfigurationBuilder();
            IConfiguration configuration = builder.Build();
            _fileStorage = new AzureFileStorageServiceCommon(configuration);
            _fileStorage.SetConnectionString(azureConnectionString);
            _tableStorage = new AzureTableStorageServiceCommon(configuration);
            _tableStorage.SetConnectionString(azureConnectionString); 
            _dbConn = new DbUtilities();

            var config = new MapperConfiguration(cfg => {
                cfg.CreateMap<SourceEntity, ConnectParameters>().ForMember(dest => dest.SourceName, opt => opt.MapFrom(src => src.RowKey));
                cfg.CreateMap<ConnectParameters, SourceEntity>().ForMember(dest => dest.RowKey, opt => opt.MapFrom(src => src.SourceName));
            });
            _mapper = config.CreateMapper();
        }

        public async Task<string> GetRules(string sourceName)
        {
            string result = "";
            ConnectParameters connector = await Common.GetConnectParameters(azureConnectionString, sourceName);
            using (IDbConnection cnn = new SqlConnection(connector.ConnectionString))
            {
                string select = getSql;
                var rules = cnn.Query<RuleModel>(select);
                result = JsonConvert.SerializeObject(rules, Formatting.Indented);
            }
            return result;
        }

        public async Task<string> GetRule(string sourceName, int id)
        {
            string result = "";
            ConnectParameters connector = await Common.GetConnectParameters(azureConnectionString, sourceName);
            RuleModel rule = SelectRule(id, connector.ConnectionString);
            result = JsonConvert.SerializeObject(rule, Formatting.Indented);
            return result;
        }

        public async Task<string> GetRuleByQuery(string sourceName, string query)
        {
            string result = "";
            ConnectParameters connector = await Common.GetConnectParameters(azureConnectionString, sourceName);
            List<RuleModel> rules = SelectRuleByQuery(query, connector.ConnectionString);
            result = JsonConvert.SerializeObject(rules, Formatting.Indented);
            return result;
        }

        public async Task<string> GetFunctionByName(string sourceName, string name)
        {
            string result = "";
            ConnectParameters connector = await Common.GetConnectParameters(azureConnectionString, sourceName);
            RuleFunctions function = SelectFunctionByName(name, connector.ConnectionString);
            result = JsonConvert.SerializeObject(function, Formatting.Indented);
            return result;
        }

        public async Task<string> GetPredictions()
        {
            string result = "";
            List<PredictionEntity> predictionEntities = await _tableStorage.GetTableRecords<PredictionEntity>(predictionContainer);
            List<PredictionSet> predictionSets = new List<PredictionSet>();
            foreach (PredictionEntity entity in predictionEntities)
            {
                predictionSets.Add(new PredictionSet()
                {
                    Name = entity.RowKey,
                    Description = entity.Decsription
                });
            }
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
            InsertRule(rule, connector.ConnectionString);
        }

        public async Task SavePredictionSet(string name, PredictionSet set)
        {
            List<RuleModel> rules = set.RuleSet;
            PredictionEntity tmpEntity = await _tableStorage.GetTableRecord<PredictionEntity>(predictionContainer, name);
            if (tmpEntity != null)
            {
                Exception error = new Exception($"RuleManagement: prediction set already exist");
                throw error;
            }
            string fileName = name + ".json";
            string json = JsonConvert.SerializeObject(rules, Formatting.Indented);
            string url = await _fileStorage.SaveFileUri(ruleShare, fileName, json);
            PredictionEntity predictionEntity = new PredictionEntity(name)
            {
                RuleUrl = url,
                Decsription = set.Description
            };
            await _tableStorage.SaveTableRecord(predictionContainer, name, predictionEntity);
        }

        public async Task UpdateRule(string name, int id, RuleModel rule)
        {
            ConnectParameters connector = await Common.GetConnectParameters(azureConnectionString, name);
            if (connector.SourceType != "DataBase")
            {
                Exception error = new Exception($"RuleManagement: data source must be a Database type");
                throw error;
            }
            RuleModel oldRule = SelectRule(id, connector.ConnectionString);
            if (oldRule == null)
            {
                Exception error = new Exception($"RuleManagement: could not find rule");
                throw error;
            }
            else
            {
                rule.Id = id;
                UpdateRule(rule, connector.ConnectionString);
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
            using (var cnn = new SqlConnection(connector.ConnectionString))
            {
                string sql = "DELETE FROM pdo_qc_rules WHERE Id = @Id";
                int recordsAffected = cnn.Execute(sql, new { Id = id });
            }
        }

        public async Task DeletePrediction(string name)
        {
            await _tableStorage.DeleteTable(predictionContainer, name);
            string ruleFile = name + ".json";
            await _fileStorage.DeleteFile(ruleShare, ruleFile);
        }

        private RuleModel SelectRule(int id, string connectionString)
        {
            RuleModel rule = new RuleModel();
            using (IDbConnection cnn = new SqlConnection(connectionString))
            {
                string query = $" where Id = {id}";
                string sql = getSql + query;
                var rules = cnn.Query<RuleModel>(sql);
                rule = rules.FirstOrDefault();
            }
            return rule;
        }

        private List<RuleModel> SelectRuleByQuery(string query, string connectionString)
        {
            List<RuleModel> rules = new List<RuleModel>();
            using (IDbConnection cnn = new SqlConnection(connectionString))
            {
                string sql = getSql + query;
                rules = cnn.Query<RuleModel>(sql).ToList();
            }
            return rules;
        }

        private RuleFunctions SelectFunctionByName(string functionName, string connectionString)
        {
            RuleFunctions function = new RuleFunctions();
            using (IDbConnection cnn = new SqlConnection(connectionString))
            {
                string sql = functionSql + $" where FunctionName = '{functionName}'";
                var functions = cnn.Query<RuleFunctions>(sql);
                function = functions.FirstOrDefault();
            }
            return function;
        }

        private void UpdateRule(RuleModel rule, string connectionString)
        {
            string userName = CommonDbUtilities.GetUsername(connectionString);
            rule.ModifiedBy = userName;
            string json = JsonConvert.SerializeObject(rule, Formatting.Indented);
            json = Common.SetJsonDataObjectDate(json, "ModifiedDate");
            using (IDbConnection cnn = new SqlConnection(connectionString))
            {
                var p = new DynamicParameters();
                p.Add("@json", json);
                string sql = "dbo.spUpdateRules";
                int recordsAffected = cnn.Execute(sql, p, commandType: CommandType.StoredProcedure);
            }
        }

        private void InsertRule(RuleModel rule, string connectionString)
        {
            string userName = CommonDbUtilities.GetUsername(connectionString);
            rule.ModifiedBy = userName;
            rule.CreatedBy = userName;
            string json = GetRuleKey(rule, connectionString);
            json = Common.SetJsonDataObjectDate(json, "CreatedDate");
            json = Common.SetJsonDataObjectDate(json, "ModifiedDate");
            using (IDbConnection cnn = new SqlConnection(connectionString))
            {
                var p = new DynamicParameters();
                p.Add("@json", json);
                string sql = "dbo.spInsertRules";
                int recordsAffected = cnn.Execute(sql, p, commandType: CommandType.StoredProcedure);
            }
        }

        private string GetRuleKey(RuleModel rule, string connectionString)
        {
            string json = "";
            RuleModel outRule = rule;
            using (IDbConnection cnn = new SqlConnection(connectionString))
            {
                string query = $" where RuleType = '{rule.RuleType}' order by keynumber desc";
                string sql = getSql + query;
                var rules = cnn.Query<RuleModel>(sql);
                outRule.KeyNumber = rules.Select(s => s.KeyNumber).FirstOrDefault() + 1;
                string strKey = outRule.KeyNumber.ToString();
                RuleTypeDictionary rt = new RuleTypeDictionary();
                outRule.RuleKey = rt[rule.RuleType] + strKey;
                json = JsonConvert.SerializeObject(outRule, Formatting.Indented);
            }
            return json;
        }
    }
}
