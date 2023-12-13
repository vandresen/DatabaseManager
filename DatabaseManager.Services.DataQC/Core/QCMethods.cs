using DatabaseManager.Services.DataQC.Extensions;
using DatabaseManager.Services.DataQC.Models;
using DatabaseManager.Services.DataQC.Services;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Data;
using System.Text.RegularExpressions;

namespace DatabaseManager.Services.DataQC.Core
{
    static class QCMethods
    {
        private static Regex isNumberTest = new Regex("^[0-9]+$", RegexOptions.Compiled);

        public static string Entirety(QcRuleSetup qcSetup, List<IndexDto> dt, List<DataAccessDef> accessDefs, IIndexAccess idxAccess)
        {
            string returnStatus = "Failed";
            if (qcSetup.IndexId == qcSetup.EniretyIndexId) returnStatus = "Passed";
            return returnStatus;
        }

        public static string Completeness(QcRuleSetup qcSetup, List<IndexDto> dt, List<DataAccessDef> accessDefs, IIndexAccess idxAccess)
        {
            string returnStatus = "Passed";
            RuleModelDto rule = JsonConvert.DeserializeObject<RuleModelDto>(qcSetup.RuleObject);
            List<IndexDto> idxRow = dt.Where(obj => obj.IndexId == qcSetup.IndexId).ToList();
            if (idxRow.Count == 1)
            {
                string jsonDataObject = idxRow[0].JsonDataObject;
                JObject dataObject = JObject.Parse(jsonDataObject);
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
            }  
            return returnStatus;
        }

        public static string Uniqueness(QcRuleSetup qcSetup, List<IndexDto> dt, List<DataAccessDef> accessDefs, IIndexAccess idxAccess)
        {
            string returnStatus = "Passed";

            List<IndexDto> idxRow = dt.Where(obj => obj.IndexId == qcSetup.IndexId).ToList();

            if (idxRow.Count == 1)
            {
                string key = idxRow[0].UniqKey;
                if (!string.IsNullOrEmpty(key))
                {
                    List<IndexDto> dups = dt.Where(obj => obj.UniqKey == key).ToList();
                    if (dups.Count > 1) returnStatus = "Failed";
                }
            }

            return returnStatus;
        }

        public static string Consistency(QcRuleSetup qcSetup, List<IndexDto> dt, List<DataAccessDef> accessDefs, IIndexAccess idxAccess)
        {
            string returnStatus = "Passed";
            RuleModelDto rule = JsonConvert.DeserializeObject<RuleModelDto>(qcSetup.RuleObject);
            List<IndexDto> idxRow = dt.Where(obj => obj.IndexId == qcSetup.IndexId).ToList();
            if (idxRow.Count == 1)
            {
                string jsonDataObject = idxRow[0].JsonDataObject;
                JObject dataObject = JObject.Parse(jsonDataObject);
                JToken value = dataObject.GetValue(rule.DataAttribute);
                string strValue = value.ToString();
                if (strValue.CompletenessCheck() == "Passed")
                {
                    string dataType = rule.DataType;
                    string query = " where " + idxRow[0].DataKey;
                    DataAccessDef dataAccessDef = accessDefs.First(x => x.DataType == dataType);
                    string sql = dataAccessDef.Select + query;
                    IDataAccess da = new ADODataAccess();
                    DataTable ct = da.GetDataTable(sql, qcSetup.ConsistencyConnectorString);
                    Dictionary<string, string> columnTypes = ct.GetColumnTypes();
                    if (ct.Rows.Count > 0)
                    {
                        string strRefValue = ct.Rows[0][rule.DataAttribute].ToString();
                        string valueType = columnTypes[rule.DataAttribute];
                        if (strRefValue.CompletenessCheck() == "Passed")
                        {
                            returnStatus = strValue.ConsistencyCheck(strRefValue, valueType);
                        }
                    }
                }
            }
            return returnStatus;
        }

        //public static string ValidityRange(QcRuleSetup qcSetup, DataTable dt, List<DataAccessDef> accessDefs, IIndexDBAccess indexData)
        //{
        //    string returnStatus = "Passed";

        //    bool canConvert;
        //    double minRange;
        //    double maxRange;
        //    RuleModel rule = JsonConvert.DeserializeObject<RuleModel>(qcSetup.RuleObject);
        //    JObject parameterObject = JObject.Parse(rule.RuleParameters);
        //    JToken jMinRangeValue = parameterObject.GetValue("MinRange");
        //    canConvert = double.TryParse(jMinRangeValue.ToString(), out minRange);
        //    if (!canConvert) minRange = -99999.0;
        //    JToken jMaxRangeValue = parameterObject.GetValue("MaxRange");
        //    canConvert = double.TryParse(jMaxRangeValue.ToString(), out maxRange);
        //    if (!canConvert) maxRange = 99999.0;

        //    JObject dataObject = JObject.Parse(qcSetup.DataObject);
        //    JToken value = dataObject.GetValue(rule.DataAttribute);
        //    double? number = value.GetNumberFromJToken();
        //    if (number != null)
        //    {
        //        if (number >= minRange & number <= maxRange)
        //        {
        //            returnStatus = "Passed";
        //        }
        //        else
        //        {
        //            returnStatus = "Failed";
        //        }
        //    }

        //    return returnStatus;
        //}

        //public static string CurveSpikes(QcRuleSetup qcSetup, DataTable dt, List<DataAccessDef> accessDefs, IIndexDBAccess indexData)
        //{
        //    string returnStatus = "Passed";
        //    string error = "";
        //    RuleModel rule = JsonConvert.DeserializeObject<RuleModel>(qcSetup.RuleObject);
        //    JObject dataObject = JObject.Parse(qcSetup.DataObject);
        //    double nullValue = Common.GetLogNullValue(dataObject.ToString());
        //    CurveSpikeParameters spikeParams = new CurveSpikeParameters()
        //    {
        //        WindowSize = 5,
        //        SeveritySize = 4
        //    };
        //    if (!string.IsNullOrEmpty(rule.RuleParameters))
        //    {
        //        try
        //        {
        //            spikeParams = JsonConvert.DeserializeObject<CurveSpikeParameters>(rule.RuleParameters);
        //            if (spikeParams.WindowSize == 0) spikeParams.WindowSize = 5;
        //            if (spikeParams.SeveritySize == 0) spikeParams.SeveritySize = 4;
        //        }
        //        catch (Exception ex)
        //        {
        //            error = $"Bad Json, {ex}";
        //        }

        //    }
        //    spikeParams.NullValue = nullValue;
        //    List<double> measuredValues = new List<double>();
        //    try
        //    {
        //        JToken value = dataObject.GetValue("MEASURED_VALUE");
        //        string strValue = value.ToString();
        //        if (!string.IsNullOrEmpty(strValue))
        //        {
        //            measuredValues = strValue.Split(',').Select(double.Parse).ToList();
        //        }
        //    }
        //    catch (Exception ex)
        //    {
        //        error = $"Problems with the log arrays, {ex}";
        //    }

        //    bool spike = RuleMethodUtilities.CurveHasSpikes(measuredValues, spikeParams);
        //    if (spike) returnStatus = "Failed";

        //    return returnStatus;
        //}

        //public static string IsNumber(QcRuleSetup qcSetup, DataTable dt, List<DataAccessDef> accessDefs, IIndexDBAccess indexData)
        //{
        //    string returnStatus = "Passed";
        //    RuleModel rule = JsonConvert.DeserializeObject<RuleModel>(qcSetup.RuleObject);
        //    JObject dataObject = JObject.Parse(qcSetup.DataObject);
        //    JToken value = dataObject.GetValue(rule.DataAttribute);
        //    if (value == null)
        //    {
        //        //log.Warning($"Attribute is Null");
        //    }
        //    else
        //    {
        //        string strValue = value.ToString();
        //        bool isValid = false;
        //        isValid = isNumberTest.IsMatch(strValue);
        //        if (!isValid) returnStatus = "Failed";
        //    }
        //    return returnStatus;
        //}

        //public static string StringLength(QcRuleSetup qcSetup, DataTable dt, List<DataAccessDef> accessDefs, IIndexDBAccess indexData)
        //{
        //    string returnStatus = "Failed";
        //    string error = "";
        //    RuleModel rule = JsonConvert.DeserializeObject<RuleModel>(qcSetup.RuleObject);
        //    JObject dataObject = JObject.Parse(qcSetup.DataObject);
        //    JToken value = dataObject.GetValue(rule.DataAttribute);
        //    StringLengthParameters stringParams = new StringLengthParameters()
        //    {
        //        Min = 20,
        //        Max = 20
        //    };
        //    if (!string.IsNullOrEmpty(rule.RuleParameters))
        //    {
        //        try
        //        {
        //            stringParams = JsonConvert.DeserializeObject<StringLengthParameters>(rule.RuleParameters);
        //            if (stringParams.Min == 0) stringParams.Min = 20;
        //            if (stringParams.Max == 0) stringParams.Max = stringParams.Min;
        //        }
        //        catch (Exception ex)
        //        {
        //            error = $"Bad parameter Json, {ex}";
        //        }

        //    }
        //    string strValue = value.ToString();
        //    strValue = strValue.Trim();
        //    int stringLength = strValue.Length;
        //    if (stringLength >= stringParams.Min & stringLength <= stringParams.Max)
        //    {
        //        returnStatus = "Passed";
        //    }
        //    else
        //    {
        //        returnStatus = "Failed";
        //    }
        //    return returnStatus;
        //}

        //public static string IsEqualTo(QcRuleSetup qcSetup, DataTable dt, List<DataAccessDef> accessDefs, IIndexDBAccess indexData)
        //{
        //    string returnStatus = "Passed";
        //    string error = "";
        //    RuleModel rule = JsonConvert.DeserializeObject<RuleModel>(qcSetup.RuleObject);
        //    IsEqualToParameters parms = new IsEqualToParameters();
        //    string[] values = new string[] { "" };
        //    if (string.IsNullOrEmpty(rule.RuleParameters))
        //    {
        //        //log.Warning($"Missing rule parameter");
        //    }
        //    else
        //    {
        //        try
        //        {
        //            parms = JsonConvert.DeserializeObject<IsEqualToParameters>(rule.RuleParameters);
        //            char delim = ',';
        //            if (!string.IsNullOrEmpty(parms.Delimiter))
        //            {
        //                if (parms.Delimiter.Length == 1)
        //                {
        //                    delim = char.Parse(parms.Delimiter);
        //                }
        //            }
        //            if (!string.IsNullOrEmpty(parms.Value))
        //            {
        //                values = parms.Value.Split(delim);
        //            }
        //        }
        //        catch (Exception ex)
        //        {
        //            error = $"Bad parameter Json, {ex}";
        //        }
        //    }
        //    JObject dataObject = JObject.Parse(qcSetup.DataObject);
        //    JToken value = dataObject.GetValue(rule.DataAttribute);
        //    if (value == null)
        //    {
        //        //log.Warning($"Attribute is Null");
        //    }
        //    else
        //    {
        //        returnStatus = "Failed";
        //        string strValue = value.ToString().Trim();
        //        foreach (string item in values)
        //        {
        //            if (strValue == item.Trim())
        //            {
        //                returnStatus = "Passed";
        //            }
        //        }
        //    }
        //    return returnStatus;
        //}

        //public static string IsGreaterThan(QcRuleSetup qcSetup, DataTable dt, List<DataAccessDef> accessDefs, IIndexDBAccess indexData)
        //{
        //    string returnStatus = "Passed";
        //    string error = "";
        //    RuleModel rule = JsonConvert.DeserializeObject<RuleModel>(qcSetup.RuleObject);
        //    IsGreaterThanParameters parms = new IsGreaterThanParameters();
        //    string[] values = new string[] { "" };
        //    if (string.IsNullOrEmpty(rule.RuleParameters))
        //    {
        //        //log.Warning($"Missing rule parameter");
        //    }
        //    else
        //    {
        //        try
        //        {
        //            parms = JsonConvert.DeserializeObject<IsGreaterThanParameters>(rule.RuleParameters);
        //        }
        //        catch (Exception ex)
        //        {
        //            error = $"Bad parameter Json, {ex}";
        //        }
        //    }
        //    JObject dataObject = JObject.Parse(qcSetup.DataObject);
        //    JToken value = dataObject.GetValue(rule.DataAttribute);
        //    if (value == null)
        //    {
        //        //log.Warning($"Attribute is Null");
        //    }
        //    else
        //    {
        //        double? number = value.GetNumberFromJToken();
        //        if (number != null)
        //        {
        //            if (number < parms.Value)
        //            {
        //                returnStatus = "Failed";
        //            }
        //        }
        //    }
        //    return returnStatus;
        //}
    }
}
