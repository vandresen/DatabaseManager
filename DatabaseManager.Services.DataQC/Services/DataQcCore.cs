using DatabaseManager.Services.DataQC.Core;
using DatabaseManager.Services.DataQC.Extensions;
using DatabaseManager.Services.DataQC.Models;
using Microsoft.VisualBasic.FileIO;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace DatabaseManager.Services.DataQC.Services
{
    public class DataQcCore: IDataQc
    {
        private readonly IIndexAccess _idxAccess;
        private readonly IConfigFileService _fs;
        private readonly IDataSourceService _ds;
        private List<DataAccessDef> _accessDefs;

        public DataQcCore(IIndexAccess idxAccess, IConfigFileService fs, IDataSourceService ds)
        {
            _idxAccess = idxAccess;
            _fs = fs;
            _ds = ds;
        }

        public async Task<List<int>> QualityCheckDataType(DataQCParameters parms, List<IndexDto> indexes, RuleModelDto rule)
        {
            if (rule.RuleType == "Predictions")
            {
                Exception error = new Exception($"Prediction rules are not allowed for Data QC ");
                throw error;
            }
            List<int> failedObjects = new List<int>();
            QcRuleSetup qcSetup = new QcRuleSetup();
            string jsonRules = JsonConvert.SerializeObject(rule);
            qcSetup.RuleObject = jsonRules;
            string ruleFilter = rule.RuleFilter;
            bool externalQcMethod = rule.RuleFunction.StartsWith("http");
            ResponseDto conFileResponse = await _fs.GetConfigurationFileAsync<ResponseDto>("PPDMDataAccess.json");
            if (conFileResponse.IsSuccess == false)
            {
                Exception error = new Exception($"Could not get the configuration file for PPDM DataAccess ");
                throw error;
            }
            _accessDefs = JsonConvert.DeserializeObject< List < DataAccessDef >> (Convert.ToString(conFileResponse.Result));

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
                if (!string.IsNullOrEmpty(jsonData))
                {
                    qcSetup.IndexId = idxRow.IndexId;
                    if (rule.RuleFunction == "Entirety")
                    {
                        bool isIdFound = entiretyList.Any(obj => obj.IndexID == idxRow.IndexId);
                        if (isIdFound) qcSetup.EniretyIndexId = idxRow.IndexId;
                    }
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
                            result = (string)info.Invoke(null, new object[] { qcSetup, indexes, _accessDefs, _idxAccess });
                        }
                        if (result == "Failed")
                        {
                            failedObjects.Add(qcSetup.IndexId);
                        }
                    }
                }
            }

            return failedObjects;
        }

        private async Task<List<int>> GetEntiretyList(RuleModelDto rule)
        {
            List<int> entiretyList = new List<int>();
            return entiretyList;
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

        private void CalculateKey(RuleModelDto rule, List<IndexDto> indexes)
        {
            string[] keyAttributes = rule.RuleParameters.Split(';');
            if (keyAttributes.Length > 0)
            {
                foreach (IndexDto idxRow in indexes)
                {
                    string keyText = "";
                    if (!string.IsNullOrEmpty(idxRow.JsonDataObject))
                    {
                        JObject dataObject = JObject.Parse(idxRow.JsonDataObject);
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
    }
}
