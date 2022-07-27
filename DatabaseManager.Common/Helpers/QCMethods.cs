using DatabaseManager.Common.Data;
using DatabaseManager.Common.DBAccess;
using DatabaseManager.Common.Entities;
using DatabaseManager.Common.Extensions;
using DatabaseManager.Shared;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using static DatabaseManager.Common.Helpers.RuleMethodUtilities;

namespace DatabaseManager.Common.Helpers
{
    static class QCMethods
    {
        private static Regex isNumberTest = new Regex("^[0-9]+$", RegexOptions.Compiled);

        public static string Entirety(QcRuleSetup qcSetup, DataTable dt, List<DataAccessDef> accessDefs, IIndexDBAccess indexData)
        {
            string returnStatus = "Passed";
            RuleModel rule = JsonConvert.DeserializeObject<RuleModel>(qcSetup.RuleObject);
            string indexNode = qcSetup.IndexNode;
            string dataName = rule.RuleParameters;
            IEnumerable<IndexModel> indexModels = Task.Run(() => indexData.GetChildrenWithName(qcSetup.DataConnector, indexNode, dataName)).GetAwaiter().GetResult();
            if (indexModels.Count() == 0) returnStatus = "Failed";
            return returnStatus;
        }

        public static string Completeness(QcRuleSetup qcSetup, DataTable dt, List<DataAccessDef> accessDefs, IIndexDBAccess indexData)
        {
            string returnStatus = "Passed";
            RuleModel rule = JsonConvert.DeserializeObject<RuleModel>(qcSetup.RuleObject);
            JObject dataObject = JObject.Parse(qcSetup.DataObject);
            JToken value = dataObject.GetValue(rule.DataAttribute);
            if (value == null)
            {
                //log.Warning($"Attribute is Null");
            }
            else
            {
                string strValue = value.ToString();
                if (string.IsNullOrWhiteSpace(strValue))
                {
                    returnStatus = "Failed";
                }
                else
                {
                    double number;
                    bool canConvert = double.TryParse(strValue, out number);
                    if (canConvert)
                    {
                        if (number == -99999) returnStatus = "Failed";
                    }
                }
            }
            return returnStatus;
        }

        public static string Uniqueness(QcRuleSetup qcSetup, DataTable dt, List<DataAccessDef> accessDefs, IIndexDBAccess indexData)
        {
            string returnStatus = "Passed";

            string query = $"INDEXID = '{qcSetup.IndexId}'";
            DataRow[] idxRows = dt.Select(query);
            if (idxRows.Length == 1)
            {
                string key = idxRows[0]["UNIQKEY"].ToString();
                if (!string.IsNullOrEmpty(key))
                {
                    query = $"UNIQKEY = '{key}'";
                    DataRow[] dtRows = dt.Select(query);
                    if (dtRows.Length > 1) returnStatus = "Failed";
                }
            }

            return returnStatus;
        }

        public static string Consistency(QcRuleSetup qcSetup, DataTable dt, List<DataAccessDef> accessDefs, IIndexDBAccess indexData)
        {
            string returnStatus = "Passed";
            RuleModel rule = JsonConvert.DeserializeObject<RuleModel>(qcSetup.RuleObject);
            JObject dataObject = JObject.Parse(qcSetup.DataObject);
            JToken value = dataObject.GetValue(rule.DataAttribute);
            string strValue = value.ToString();
            if (Common.CompletenessCheck(strValue) == "Passed")
            {
                string dataType = rule.DataType;
                IndexModel index = Task.Run(() => indexData.GetIndex(qcSetup.IndexId, qcSetup.DataConnector)).GetAwaiter().GetResult();
                if (index != null)
                {
                    string query = " where " + index.DataKey;
                    DataAccessDef dataAccessDef = accessDefs.First(x => x.DataType == dataType);
                    string sql = dataAccessDef.Select + query;
                    IADODataAccess consistencyConn = new ADODataAccess();
                    DataTable ct = consistencyConn.GetDataTable(sql, qcSetup.ConsistencyConnectorString);
                    Dictionary<string, string> columnTypes = ct.GetColumnTypes();
                    if (ct.Rows.Count > 0)
                    {
                        string strRefValue = ct.Rows[0][rule.DataAttribute].ToString();
                        string valueType = columnTypes[rule.DataAttribute];
                        if (Common.CompletenessCheck(strRefValue) == "Passed")
                        {
                            returnStatus = RuleMethodUtilities.ConsistencyCheck(strValue, strRefValue, valueType);
                        }
                    }
                }
            }
            return returnStatus;
        }

        public static string ValidityRange(QcRuleSetup qcSetup, DataTable dt, List<DataAccessDef> accessDefs, IIndexDBAccess indexData)
        {
            string returnStatus = "Passed";

            bool canConvert;
            double minRange;
            double maxRange;
            RuleModel rule = JsonConvert.DeserializeObject<RuleModel>(qcSetup.RuleObject);
            JObject parameterObject = JObject.Parse(rule.RuleParameters);
            JToken jMinRangeValue = parameterObject.GetValue("MinRange");
            canConvert = double.TryParse(jMinRangeValue.ToString(), out minRange);
            if (!canConvert) minRange = -99999.0;
            JToken jMaxRangeValue = parameterObject.GetValue("MaxRange");
            canConvert = double.TryParse(jMaxRangeValue.ToString(), out maxRange);
            if (!canConvert) maxRange = 99999.0;

            JObject dataObject = JObject.Parse(qcSetup.DataObject);
            JToken value = dataObject.GetValue(rule.DataAttribute);
            double? number = value.GetNumberFromJToken();
            if (number != null)
            {
                if (number >= minRange & number <= maxRange)
                {
                    returnStatus = "Passed";
                }
                else
                {
                    returnStatus = "Failed";
                }
            }

            return returnStatus;
        }

        public static string CurveSpikes(QcRuleSetup qcSetup, DataTable dt, List<DataAccessDef> accessDefs, IIndexDBAccess indexData)
        {
            string returnStatus = "Passed";
            string error = "";
            RuleModel rule = JsonConvert.DeserializeObject<RuleModel>(qcSetup.RuleObject);
            JObject dataObject = JObject.Parse(qcSetup.DataObject);
            double nullValue = Common.GetLogNullValue(dataObject.ToString());
            CurveSpikeParameters spikeParams = new CurveSpikeParameters()
            {
                WindowSize = 5,
                SeveritySize = 4
            };
            if (!string.IsNullOrEmpty(rule.RuleParameters))
            {
                try
                {
                    spikeParams = JsonConvert.DeserializeObject<CurveSpikeParameters>(rule.RuleParameters);
                    if (spikeParams.WindowSize == 0) spikeParams.WindowSize = 5;
                    if (spikeParams.SeveritySize == 0) spikeParams.SeveritySize = 4;
                }
                catch (Exception ex)
                {
                    error = $"Bad Json, {ex}";
                }

            }
            spikeParams.NullValue = nullValue;
            List<double> measuredValues = new List<double>();
            try
            {
                JToken value = dataObject.GetValue("MEASURED_VALUE");
                string strValue = value.ToString();
                if (!string.IsNullOrEmpty(strValue))
                {
                    measuredValues = strValue.Split(',').Select(double.Parse).ToList();
                }
            }
            catch (Exception ex)
            {
                error = $"Problems with the log arrays, {ex}";
            }

            bool spike = RuleMethodUtilities.CurveHasSpikes(measuredValues, spikeParams);
            if (spike) returnStatus = "Failed";

            return returnStatus;
        }

        public static string IsNumber(QcRuleSetup qcSetup, DataTable dt, List<DataAccessDef> accessDefs, IIndexDBAccess indexData)
        {
            string returnStatus = "Passed";
            RuleModel rule = JsonConvert.DeserializeObject<RuleModel>(qcSetup.RuleObject);
            JObject dataObject = JObject.Parse(qcSetup.DataObject);
            JToken value = dataObject.GetValue(rule.DataAttribute);
            if (value == null)
            {
                //log.Warning($"Attribute is Null");
            }
            else
            {
                string strValue = value.ToString();
                bool isValid = false;
                isValid = isNumberTest.IsMatch(strValue);
                if (!isValid) returnStatus = "Failed";
            }
            return returnStatus;
        }

        public static string StringLength(QcRuleSetup qcSetup, DataTable dt, List<DataAccessDef> accessDefs, IIndexDBAccess indexData)
        {
            string returnStatus = "Failed";
            string error = "";
            RuleModel rule = JsonConvert.DeserializeObject<RuleModel>(qcSetup.RuleObject);
            JObject dataObject = JObject.Parse(qcSetup.DataObject);
            JToken value = dataObject.GetValue(rule.DataAttribute);
            StringLengthParameters stringParams = new StringLengthParameters()
            {
                Min = 20,
                Max = 20
            };
            if (!string.IsNullOrEmpty(rule.RuleParameters))
            {
                try
                {
                    stringParams = JsonConvert.DeserializeObject<StringLengthParameters>(rule.RuleParameters);
                    if (stringParams.Min == 0) stringParams.Min = 20;
                    if (stringParams.Max == 0) stringParams.Max = stringParams.Min;
                }
                catch (Exception ex)
                {
                    error = $"Bad parameter Json, {ex}";
                }

            }
            string strValue = value.ToString();
            strValue = strValue.Trim();
            int stringLength = strValue.Length;
            if (stringLength >= stringParams.Min & stringLength <= stringParams.Max)
            {
                returnStatus = "Passed";
            }
            else
            {
                returnStatus = "Failed";
            }
            return returnStatus;
        }

        public static string IsEqualTo(QcRuleSetup qcSetup, DataTable dt, List<DataAccessDef> accessDefs, IIndexDBAccess indexData)
        {
            string returnStatus = "Passed";
            string error = "";
            RuleModel rule = JsonConvert.DeserializeObject<RuleModel>(qcSetup.RuleObject);
            IsEqualToParameters parms = new IsEqualToParameters();
            string[] values = new string[] { "" };
            if (string.IsNullOrEmpty(rule.RuleParameters))
            {
                //log.Warning($"Missing rule parameter");
            }
            else
            {
                try
                {
                    parms = JsonConvert.DeserializeObject<IsEqualToParameters>(rule.RuleParameters);
                    char delim = ',';
                    if (!string.IsNullOrEmpty(parms.Delimiter))
                    {
                        if (parms.Delimiter.Length == 1)
                        {
                            delim = char.Parse(parms.Delimiter);
                        }
                    }
                    if (!string.IsNullOrEmpty(parms.Value))
                    {
                        values = parms.Value.Split(delim);
                    }    
                }
                catch (Exception ex)
                {
                    error = $"Bad parameter Json, {ex}";
                }
            }
            JObject dataObject = JObject.Parse(qcSetup.DataObject);
            JToken value = dataObject.GetValue(rule.DataAttribute);
            if (value == null)
            {
                //log.Warning($"Attribute is Null");
            }
            else
            {
                returnStatus = "Failed";
                string strValue = value.ToString().Trim();
                foreach (string item in values)
                {
                    if (strValue == item.Trim())
                    {
                        returnStatus = "Passed";
                    }
                }
            }
            return returnStatus;
        }
    }
}
