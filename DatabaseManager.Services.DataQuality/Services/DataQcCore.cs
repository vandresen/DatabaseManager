using DatabaseManager.Services.DataQuality.Core;
using DatabaseManager.Services.DataQuality.Extensions;
using DatabaseManager.Services.DataQuality.Models;
using Microsoft.Extensions.Logging;
using Microsoft.VisualBasic.FileIO;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Reflection;
using System.Text;
using System.Text.Json;

namespace DatabaseManager.Services.DataQuality.Services
{
    public class DataQcCore : IDataQc
    {
        private readonly ILogger<DataQcCore> _logger;
        private readonly IConfigFileService _fs;
        private List<DataAccessDef> _accessDefs;
        private readonly IDataSourceService _ds;
        private readonly IIndexAccess _idxAccess;

        public DataQcCore(ILogger<DataQcCore> logger, IConfigFileService fs, IDataSourceService ds,
            IIndexAccess idxAccess)
        {
            _logger = logger;
            _fs = fs;
            _ds = ds;
            _idxAccess = idxAccess;
        }

        public async Task<DataQcResult> QualityCheckDataAsync(DataQCParameters parms, List<IndexDto> indexes, RuleModelDto rule)
        {
            if (rule.RuleType == "Predictions")
            {
                Exception error = new Exception($"Prediction rules are not allowed for Data QC ");
                throw error;
            }
            DataQcResult result = new ();
            List<int> failedObjects = new List<int>();

            ResponseDto conFileResponse = await _fs.GetConfigurationFileAsync<ResponseDto>("PPDMDataAccess.json");
            if (!conFileResponse.IsSuccess)
            {
                _logger.LogError("Failed to load configuration file: {Errors}", string.Join(", ", conFileResponse.ErrorMessages));
                throw new InvalidOperationException("Cannot load PPDMDataAccess.json config");
            }
            _accessDefs = JsonConvert.DeserializeObject<List<DataAccessDef>>(Convert.ToString(conFileResponse.Result));
            _logger.LogInformation($"DataQC: Number of data access defs are {_accessDefs.Count}");

            QcRuleSetup qcSetup = new QcRuleSetup();
            string jsonRules = JsonConvert.SerializeObject(rule);
            qcSetup.RuleObject = jsonRules;
            string ruleFilter = rule.RuleFilter;
            bool externalQcMethod = rule.RuleFunction.StartsWith("http");

            if (rule.RuleFunction == "Uniqueness") CalculateKey(rule, indexes);
            if (rule.RuleFunction == "Consistency") qcSetup.ConsistencyConnectorString = await GetConsistencySource(rule.RuleParameters);

            List<EntiretyListModel> entiretyList = new List<EntiretyListModel>();
            if (rule.RuleFunction == "Entirety")
            {
                JObject parameterObject = JObject.Parse(rule.RuleParameters);
                string entiretyName = parameterObject.GetValue("Name").ToString();
                string dataType = parameterObject.GetValue("DataType").ToString();
                ResponseDto entiretyResponse = await _idxAccess.GetEntiretyIndexes<ResponseDto>(parms.DataConnector, dataType,
                    entiretyName, rule.DataType);
                if (entiretyResponse.IsSuccess == true)
                {
                    entiretyList = JsonConvert.DeserializeObject<List<EntiretyListModel>>(Convert.ToString(entiretyResponse.Result));
                }
                else
                {
                    Exception error = new Exception($"Could not create a entirety list ");
                    throw error;
                }
            }

            foreach (IndexDto idxRow in indexes)
            {
                string jsonData = idxRow.JsonDataObject;
                if (string.IsNullOrEmpty(jsonData)) break;

                qcSetup.IndexId = idxRow.IndexId;

                if (rule.RuleFunction == "Entirety")
                {
                    bool isIdFound = entiretyList.Any(obj => obj.IndexID == idxRow.IndexId);
                    if (isIdFound) qcSetup.EniretyIndexId = idxRow.IndexId;
                }

                string outcome = "Passed";

                if (Filter(jsonData, ruleFilter)) break;

                if (externalQcMethod)
                {
                    outcome = ProcessQcRule(qcSetup, rule);
                }
                else
                {
                    Type type = typeof(QCMethods);
                    MethodInfo info = type.GetMethod(rule.RuleFunction);
                    outcome = (string)info.Invoke(null, new object[] { qcSetup, indexes, _accessDefs, _idxAccess });
                }
                if (outcome == "Failed")
                {
                    failedObjects.Add(qcSetup.IndexId);
                }
            }
            
            result.FailedIndexes = failedObjects;
            result.IsSuccess = true;
            return result;
        }

        private string ProcessQcRule(QcRuleSetup qcSetup, RuleModelDto rule)
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

        private bool Filter(string jsonData, string ruleFilter)
        {
            bool filter = false;
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

        private async Task<string> GetConsistencySource(string RuleParameters)
        {
            string source = string.Empty;
            ConsistencyParameters parms = new ConsistencyParameters();
            if (!string.IsNullOrEmpty(RuleParameters))
            {
                try
                {
                    parms = JsonConvert.DeserializeObject<ConsistencyParameters>(RuleParameters);
                }
                catch (Exception ex)
                {
                    Exception error = new Exception($"The consistency rule has bad parameter Json, {ex}");
                    throw error;
                }
            }
            else
            {
                Exception error = new Exception($"Missing rule parameter, the Consistence rule must have a parameter for the consistency source");
                throw error;
            }
            ResponseDto dsResponse = await _ds.GetDataSourceByNameAsync<ResponseDto>(parms.Source);
            if (dsResponse.IsSuccess)
            {
                ConnectParameters connector = JsonConvert.DeserializeObject<ConnectParameters>(Convert.ToString(dsResponse.Result));
                source = connector.ConnectionString;
            }
            else
            {
                Exception error = new Exception($"Could not find Consistence source");
                throw error;
            }
            return source;
        }

        private void CalculateKey(RuleModelDto rule, List<IndexDto> indexes)
        {
            if (string.IsNullOrWhiteSpace(rule.RuleParameters)) return;
            string[] keyAttributes = rule.RuleParameters
                .Split(';', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
            if (keyAttributes.Length == 0) return;

            foreach (IndexDto idxRow in indexes)
            {
                string keyText = "";
                if (string.IsNullOrWhiteSpace(idxRow.JsonDataObject))
                    continue;
                JObject dataObject;
                try
                {
                    dataObject = JObject.Parse(idxRow.JsonDataObject);
                }
                catch (Newtonsoft.Json.JsonException ex)
                {
                    _logger.LogWarning(ex, "Invalid JSON in IndexDto");
                    continue;
                }

                foreach (string key in keyAttributes)
                {
                    string function = "";
                    string normalizeParameter = "";
                    string attribute = key.Trim();
                    if (attribute.Substring(0, 1) == "*")
                    {
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
                    idxRow.UniqKey = key;
                }
            }
            
        }
    }
}
