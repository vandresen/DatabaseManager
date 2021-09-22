using AutoMapper;
using DatabaseManager.Common.Entities;
using DatabaseManager.Common.Services;
using DatabaseManager.Shared;
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Data;
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
            if (connector.SourceType == "DataBase")
            {
                string jsonConnectDef = await _fileStorage.ReadFile("connectdefinition", "PPDMDataAccess.json");
                connector.DataAccessDefinition = jsonConnectDef;
            }
            else
            {
                Exception error = new Exception($"RuleManagement: data source must be a Database type");
                throw error;
            }
            _dbConn.OpenConnection(connector);
            List<DataAccessDef> accessDefs = JsonConvert.DeserializeObject<List<DataAccessDef>>(connector.DataAccessDefinition);
            DataAccessDef ruleAccessDef = accessDefs.First(x => x.DataType == "Rules");
            string select = ruleAccessDef.Select;
            string query = "";
            DataTable dt = _dbConn.GetDataTable(select, query);
            result = JsonConvert.SerializeObject(dt, Formatting.Indented);
            return result;
        }

        public async Task<string> GetRule(string sourceName, int id)
        {
            string result = "";
            ConnectParameters connector = await Common.GetConnectParameters(azureConnectionString, sourceName);
            if (connector.SourceType == "DataBase")
            {
                string jsonConnectDef = await _fileStorage.ReadFile("connectdefinition", "PPDMDataAccess.json");
                connector.DataAccessDefinition = jsonConnectDef;
            }
            else
            {
                Exception error = new Exception($"RuleManagement: data source must be a Database type");
                throw error;
            }
            _dbConn.OpenConnection(connector);
            List<DataAccessDef> accessDefs = JsonConvert.DeserializeObject<List<DataAccessDef>>(connector.DataAccessDefinition);
            DataAccessDef ruleAccessDef = accessDefs.First(x => x.DataType == "Rules");
            string select = ruleAccessDef.Select;
            string query = $" where Id = {id}";
            DataTable dt = _dbConn.GetDataTable(select, query);
            string strRule = JsonConvert.SerializeObject(dt, Formatting.Indented);
            JToken ruleToken = JArray.Parse(strRule).FirstOrDefault();
            result = JsonConvert.SerializeObject(ruleToken);
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
            if (connector.SourceType == "DataBase")
            {
                string jsonConnectDef = await _fileStorage.ReadFile("connectdefinition", "PPDMDataAccess.json");
                connector.DataAccessDefinition = jsonConnectDef;
            }
            else
            {
                Exception error = new Exception($"RuleManagement: data source must be a Database type");
                throw error;
            }
            List<DataAccessDef> accessDefs = JsonConvert.DeserializeObject<List<DataAccessDef>>(connector.DataAccessDefinition);
            DataAccessDef ruleAccessDef = accessDefs.First(x => x.DataType == "Rules");
            if (rule == null)
            {
                Exception error = new Exception($"RuleManagement: Rule is missing");
                throw error;
            }

            _dbConn.OpenConnection(connector);
            InsertRule(rule, ruleAccessDef);
            _dbConn.CloseConnection();
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
            if (connector.SourceType == "DataBase")
            {
                string jsonConnectDef = await _fileStorage.ReadFile("connectdefinition", "PPDMDataAccess.json");
                connector.DataAccessDefinition = jsonConnectDef;
            }
            else
            {
                Exception error = new Exception($"RuleManagement: data source must be a Database type");
                throw error;
            }

            _dbConn.OpenConnection(connector);
            string select = "Select * from pdo_qc_rules ";
            string query = $"where Id = {id}";
            DataTable dt = _dbConn.GetDataTable(select, query);
            if (dt.Rows.Count == 1)
            {
                rule.Id = id;
                UpdateRule(rule);
            }
            else
            {
                Exception error = new Exception($"RuleManagement: could not find rule");
                throw error;
            }

            _dbConn.CloseConnection();
        }

        public async Task DeleteRule(string name, int id)
        {
            ConnectParameters connector = await Common.GetConnectParameters(azureConnectionString, name);
            if (connector.SourceType == "DataBase")
            {
                string jsonConnectDef = await _fileStorage.ReadFile("connectdefinition", "PPDMDataAccess.json");
                connector.DataAccessDefinition = jsonConnectDef;
            }
            else
            {
                Exception error = new Exception($"RuleManagement: data source must be a Database type");
                throw error;
            }

            _dbConn.OpenConnection(connector);

            string select = "Select * from pdo_qc_rules ";
            string query = $"where Id = {id}";
            DataTable dt = _dbConn.GetDataTable(select, query);
            if (dt.Rows.Count == 1)
            {
                string table = "pdo_qc_rules";
                _dbConn.DBDelete(table, query);
            }
            else
            {
                Exception error = new Exception($"RuleManagement: could not find rule");
                throw error;
            }

            _dbConn.CloseConnection();
        }

        public async Task DeletePrediction(string name)
        {
            await _tableStorage.DeleteTable(predictionContainer, name);
            string ruleFile = name + ".json";
            await _fileStorage.DeleteFile(ruleShare, ruleFile);
        }

        private void UpdateRule(RuleModel rule)
        {
            rule.ModifiedBy = _dbConn.GetUsername();
            string json = JsonConvert.SerializeObject(rule, Formatting.Indented);
            json = Common.SetJsonDataObjectDate(json, "ModifiedDate");
            _dbConn.UpdateDataObject(json, "Rules");
        }

        private void InsertRule(RuleModel rule, DataAccessDef ruleAccessDef)
        {
            string userName = _dbConn.GetUsername();
            rule.ModifiedBy = userName;
            rule.CreatedBy = userName;
            string jsonInsert = JsonConvert.SerializeObject(rule, Formatting.Indented);
            string json = GetRuleKey(jsonInsert, ruleAccessDef);
            json = Common.SetJsonDataObjectDate(json, "CreatedDate");
            json = Common.SetJsonDataObjectDate(json, "ModifiedDate");
            _dbConn.InsertDataObject(json, "Rules");
        }

        private string GetRuleKey(string jsonInput, DataAccessDef ruleAccessDef)
        {
            JObject dataObject = JObject.Parse(jsonInput);
            string category = dataObject["RuleType"].ToString();
            string select = ruleAccessDef.Select;
            string query = $" where RuleType = '{category}' order by keynumber desc";
            DataTable dt = _dbConn.GetDataTable(select, query);
            int key = 1;
            if (dt.Rows.Count > 0)
            {
                int.TryParse(dt.Rows[0]["KeyNumber"].ToString(), out key);
                if (key == 0)
                {
                    throw new System.ArgumentException("KeyNumber is bad", "original");
                }
                key++;
            }
            string strKey = key.ToString();
            dataObject["KeyNumber"] = strKey;
            RuleTypeDictionary rt = new RuleTypeDictionary();
            string ruleKey = rt[category];
            dataObject["RuleKey"] = ruleKey + strKey;
            string json = dataObject.ToString();
            return json;
        }
    }
}
