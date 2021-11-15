using AutoMapper;
using DatabaseManager.Common.Entities;
using DatabaseManager.Common.Services;
using DatabaseManager.Common.Extensions;
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
using System.Net.Http;
using System.Reflection;
using Microsoft.VisualBasic.FileIO;
using System.IO;

namespace DatabaseManager.Common.Helpers
{
    public class DataQC
    {
        private readonly IFileStorageServiceCommon _fileStorage;
        private readonly ITableStorageServiceCommon _tableStorage;
        private readonly string container = "sources";
        private readonly string _azureConnectionString;
        private List<DataAccessDef> _accessDefs;
        private DbUtilities _dbConn;
        private IMapper _mapper;
        private ManageIndexTable manageQCFlags;

        public DataQC(string azureConnectionString)
        {
            _azureConnectionString = azureConnectionString;
            var builder = new ConfigurationBuilder();
            IConfiguration configuration = builder.Build();
            _fileStorage = new AzureFileStorageServiceCommon(configuration);
            _fileStorage.SetConnectionString(azureConnectionString);
            _tableStorage = new AzureTableStorageServiceCommon(configuration);
            _tableStorage.SetConnectionString(azureConnectionString);

            _dbConn = new DbUtilities();

            var config = new MapperConfiguration(cfg => {
                cfg.CreateMap<SourceEntity, ConnectParameters>().ForMember(dest => dest.SourceName, opt => opt.MapFrom(src => src.RowKey));
            });
            _mapper = config.CreateMapper();
        }

        public async Task<List<QcResult>> GetResults(string source)
        {
            List<QcResult> result = new List<QcResult>();
            string accessJson = await _fileStorage.ReadFile("connectdefinition", "PPDMDataAccess.json");
            _accessDefs = JsonConvert.DeserializeObject<List<DataAccessDef>>(accessJson);
            ConnectParameters connector = await GetConnector(source);
            _dbConn.OpenConnection(connector);
            DataAccessDef indexAccessDef = _accessDefs.First(x => x.DataType == "Index");
            RuleManagement rules = new RuleManagement(_azureConnectionString);
            string query = " where Active = 'Y'";
            string jsonString = await rules.GetRuleByQuery(connector.SourceName, query);
            result = JsonConvert.DeserializeObject<List<QcResult>>(jsonString);
            foreach (QcResult qcItem in result)
            {
                string sql = indexAccessDef.Select;
                query = $" where QC_STRING like '%{qcItem.RuleKey};%'";
                DataTable ft = _dbConn.GetDataTable(sql, query);
                qcItem.Failures = ft.Rows.Count;
            }
            _dbConn.CloseConnection();
            return result;
        }

        public async Task<List<DmsIndex>> GetResult(string source, int id)
        {
            List<DmsIndex> result = new List<DmsIndex>();
            string accessJson = await _fileStorage.ReadFile("connectdefinition", "PPDMDataAccess.json");
            _accessDefs = JsonConvert.DeserializeObject<List<DataAccessDef>>(accessJson);
            ConnectParameters connector = await GetConnector(source);
            _dbConn.OpenConnection(connector);
            DataAccessDef indexAccessDef = _accessDefs.First(x => x.DataType == "Index");

            RuleManagement rules = new RuleManagement(_azureConnectionString);
            string jsonRule = await rules.GetRule(source, id);
            RuleModel rule = JsonConvert.DeserializeObject<RuleModel>(jsonRule);

            string sql = indexAccessDef.Select;
            string query = $" where QC_STRING like '%{rule.RuleKey};%'";
            DataTable idx = _dbConn.GetDataTable(sql, query);

            foreach (DataRow idxRow in idx.Rows)
            {
                string dataType = idxRow["DATATYPE"].ToString();
                string indexId = idxRow["INDEXID"].ToString();
                string jsonData = idxRow["JSONDATAOBJECT"].ToString();
                int intIndexId = Convert.ToInt32(indexId);
                result.Add(new DmsIndex()
                {
                    Id = intIndexId,
                    DataType = dataType,
                    JsonData = jsonData
                });
            }

            _dbConn.CloseConnection();
            return result;
        }

        public async Task<List<QcResult>> GetQCRules(DataQCParameters qcParms)
        {
            List<QcResult> qcResult = new List<QcResult>();
            string accessJson = await _fileStorage.ReadFile("connectdefinition", "PPDMDataAccess.json");
            _accessDefs = JsonConvert.DeserializeObject<List<DataAccessDef>>(accessJson);
            ConnectParameters connector = await GetConnector(qcParms.DataConnector);
            _dbConn.OpenConnection(connector);
            DataAccessDef indexAccessDef = _accessDefs.First(x => x.DataType == "Index");
            RuleManagement rules = new RuleManagement(_azureConnectionString);
            string query = " where Active = 'Y' and RuleType != 'Predictions'";
            string jsonString = await rules.GetRuleByQuery(connector.SourceName, query);
            qcResult = JsonConvert.DeserializeObject<List<QcResult>>(jsonString);
            foreach (QcResult qcItem in qcResult)
            {
                string sql = indexAccessDef.Select;
                query = $" where QC_STRING like '%{qcItem.RuleKey};%'";
                DataTable ft = _dbConn.GetDataTable(sql, query);
                qcItem.Failures = ft.Rows.Count;
            }
            _dbConn.CloseConnection();
            return qcResult;
        }

        public async Task<string> GetQCFailures(string source, int id)
        {
            List<DmsIndex> qcIndex = new List<DmsIndex>();
            ConnectParameters connector = await GetConnector(source);
            _dbConn.OpenConnection(connector);
            RuleManagement rules = new RuleManagement(_azureConnectionString);
            string jsonRule = await rules.GetRule(source, id);
            RuleModel rule = JsonConvert.DeserializeObject<RuleModel>(jsonRule);

            string accessJson = await _fileStorage.ReadFile("connectdefinition", "PPDMDataAccess.json");
            _accessDefs = JsonConvert.DeserializeObject<List<DataAccessDef>>(accessJson);
            DataAccessDef ruleAccessDef = _accessDefs.First(x => x.DataType == "Index");
            string sql = ruleAccessDef.Select;
            string query = $" where QC_STRING like '%{rule.RuleKey};%'";
            DataTable idx = _dbConn.GetDataTable(sql, query);
            foreach (DataRow idxRow in idx.Rows)
            {
                string dataType = idxRow["DATATYPE"].ToString();
                string indexId = idxRow["INDEXID"].ToString();
                string jsonData = idxRow["JSONDATAOBJECT"].ToString();
                int intIndexId = Convert.ToInt32(indexId);
                qcIndex.Add(new DmsIndex()
                {
                    Id = intIndexId,
                    DataType = dataType,
                    JsonData = jsonData
                });
            }
            string result = JsonConvert.SerializeObject(qcIndex);
            _dbConn.CloseConnection();
            return result;
        }

        public async Task ClearQCFlags(string source)
        {
            try
            {
                ConnectParameters connector = await GetConnector(source);

                DbUtilities dbConn = new DbUtilities();
                dbConn.OpenConnection(connector);
                dbConn.SQLExecute("EXEC spClearQCFlags");
                dbConn.CloseConnection();
            }
            catch (Exception ex)
            { 
                Exception error = new Exception($"DataQc: Could not clear qc flags, {ex}");
                throw error;
            }
        }

        public async Task ProcessQcRule(DataQCParameters qcParms)
        {
            try
            {
                ConnectParameters connector = await GetConnector(qcParms.DataConnector);

                string accessJson = await _fileStorage.ReadFile("connectdefinition", "PPDMDataAccess.json");
                _accessDefs = JsonConvert.DeserializeObject<List<DataAccessDef>>(accessJson);

                RuleManagement rules = new RuleManagement(_azureConnectionString);
                string jsonRule = await rules.GetRule(qcParms.DataConnector, qcParms.RuleId);
                RuleModel rule = JsonConvert.DeserializeObject<RuleModel>(jsonRule);
                string jsonFunction = await rules.GetFunctionByName(qcParms.DataConnector, rule.RuleFunction);
                RuleFunctions function = JsonConvert.DeserializeObject<RuleFunctions>(jsonFunction);
                string functionKey = "";
                if (!string.IsNullOrEmpty(function.FunctionKey)) functionKey = "?code=" + function.FunctionKey;
                rule.RuleFunction = function.FunctionUrl + functionKey;

                DbUtilities dbConn = new DbUtilities();
                dbConn.OpenConnection(connector);
                manageQCFlags = new ManageIndexTable(_accessDefs, connector.ConnectionString, rule.DataType);
                manageQCFlags.InitQCFlags(qcParms.ClearQCFlags);
                await QualityCheckDataType(dbConn, rule, connector);
                dbConn.CloseConnection();
                manageQCFlags.SaveQCFlags();
            }
            catch (Exception ex)
            {
                Exception error = new Exception($"DataQc: Could process rule {qcParms.RuleId}, {ex}");
                throw error;
            }
        }

        private async Task<ConnectParameters> GetConnector(string connectorStr)
        {
            if (String.IsNullOrEmpty(connectorStr))
            {
                Exception error = new Exception($"DataQc: Connection string is not set");
                throw error;
            }
            ConnectParameters connector = new ConnectParameters();
            SourceEntity entity = await _tableStorage.GetTableRecord<SourceEntity>(container, connectorStr);
            if (entity == null)
            {
                Exception error = new Exception($"DataQc: Could not find source connector");
                throw error;
            }
            connector = _mapper.Map<ConnectParameters>(entity);

            return connector;
        }

        private async Task QualityCheckDataType(DbUtilities dbConn, RuleModel rule, ConnectParameters connector)
        {
            QcRuleSetup qcSetup = new QcRuleSetup();
            qcSetup.Database = connector.Catalog;
            qcSetup.DatabasePassword = connector.Password;
            qcSetup.DatabaseServer = connector.DatabaseServer;
            qcSetup.DatabaseUser = connector.User;
            DataAccessDef ruleAccessDef = _accessDefs.First(x => x.DataType == "Index");
            string ruleFilter = rule.RuleFilter;
            string jsonRules = JsonConvert.SerializeObject(rule);
            qcSetup.RuleObject = jsonRules;
            bool externalQcMethod = rule.RuleFunction.StartsWith("http");

            DataTable indexTable = manageQCFlags.GetIndexTable();
            if (rule.RuleFunction == "Uniqueness") CalculateKey(rule, indexTable);
            if (rule.RuleFunction == "Consistency") qcSetup.ConsistencyConnectorString = await GetConsistencySource(rule.RuleParameters);
            foreach (DataRow idxRow in indexTable.Rows)
            {
                string jsonData = idxRow["JSONDATAOBJECT"].ToString();
                if (!string.IsNullOrEmpty(jsonData))
                {
                    qcSetup.IndexId = Convert.ToInt32(idxRow["INDEXID"]);
                    qcSetup.IndexNode = idxRow["Text_IndexNode"].ToString();
                    string qcStr = manageQCFlags.GetQCFlag(qcSetup.IndexId);
                    qcSetup.DataObject = jsonData;
                    string result = "Passed";
                    if (!Filter(jsonData, ruleFilter))
                    {
                        if (externalQcMethod)
                        {
                            result = ProcessQcRule(qcSetup, rule);
                        }
                        else
                        {
                            Type type = typeof(QCMethods);
                            MethodInfo info = type.GetMethod(rule.RuleFunction);
                            result = (string)info.Invoke(null, new object[] { qcSetup, dbConn, indexTable, _accessDefs });
                        }
                        if (result == "Failed")
                        {
                            qcStr = qcStr + rule.RuleKey + ";";
                            manageQCFlags.SetQCFlag(qcSetup.IndexId, qcStr);
                        }
                    }
                }
            }
        }

        private void CalculateKey(RuleModel rule, DataTable indexTable)
        {
            string[] keyAttributes = rule.RuleParameters.Split(';');
            if (keyAttributes.Length > 0)
            {
                foreach (DataRow idxRow in indexTable.Rows)
                {
                    string keyText = "";
                    string jsonData = idxRow["JSONDATAOBJECT"].ToString();
                    if (!string.IsNullOrEmpty(jsonData))
                    {
                        JObject dataObject = JObject.Parse(jsonData);
                        foreach (string key in keyAttributes)
                        {
                            string function = "";
                            string normalizeParameter = "";
                            string attribute = key.Trim();
                            if (attribute.Substring(0, 1) == "*")
                            {
                                //attribute = attribute.Split('(', ')')[1];
                                int start = attribute.IndexOf("(") + 1;
                                int end = attribute.IndexOf(")", start);
                                function = attribute.Substring(0, start - 1);
                                string csv = attribute.Substring(start, end - start);
                                TextFieldParser parser = new TextFieldParser(new StringReader(csv));
                                parser.HasFieldsEnclosedInQuotes = true;
                                parser.SetDelimiters(",");
                                string[] parms = parser.ReadFields();
                                attribute = parms[0];
                                if (parms.Length > 1) normalizeParameter = parms[1];
                            }
                            string value = dataObject.GetValue(attribute).ToString();
                            if (function == "*NORMALIZE") value = value.NormalizeString(normalizeParameter);
                            if (function == "*NORMALIZE14") value = value.NormalizeString14();
                            keyText = keyText + value;
                        }
                        if (!string.IsNullOrEmpty(keyText))
                        {
                            string key = keyText.GetSHA256Hash();
                            idxRow["UNIQKEY"] = key;
                        }
                    }
                }
            }
        }

        private async Task<string> GetConsistencySource(string RuleParameters)
        {
            SourceEntity connector = new SourceEntity();
            connector = await _tableStorage.GetTableRecord<SourceEntity>(container, RuleParameters);
            string source = connector.ConnectionString;
            return source;
        }

        private Boolean Filter(string jsonData, string ruleFilter)
        {
            Boolean filter = false;
            if (!string.IsNullOrEmpty(ruleFilter))
            {
                string[] filterValues = ruleFilter.Split('=');
                if (filterValues.Length == 2)
                {
                    JObject json = JObject.Parse(jsonData);
                    string dataAttribute = filterValues[0].Trim();
                    string dataValue = filterValues[1].Trim();
                    if (json[dataAttribute].ToString() != dataValue) filter = true;
                }
                else
                {
                    filter = true;
                }
            }
            return filter;
        }

        private string ProcessQcRule(QcRuleSetup qcSetup, RuleModel rule)
        {
            string returnResult = "Passed";
            var jsonString = JsonConvert.SerializeObject(qcSetup);
            var content = new StringContent(jsonString, Encoding.UTF8, "application/json");
            try
            {
                using (HttpClient client = new HttpClient())
                {
                    using (HttpResponseMessage response = client.PostAsync(rule.RuleFunction, content).Result)
                    {
                        using (HttpContent respContent = response.Content)
                        {
                            var tr = respContent.ReadAsStringAsync().Result;
                            var azureResponse = JsonConvert.DeserializeObject(tr);
                            returnResult = azureResponse.ToString();
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Exception error = new Exception("DataQcWithProgressBar: Problems with URL: ", ex);
                throw error;
            }
            return returnResult;
        }
    }
}
