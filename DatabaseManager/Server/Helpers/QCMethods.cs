using DatabaseManager.Shared;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Threading.Tasks;

namespace DatabaseManager.Server.Helpers
{
    static class QCMethods
    {

        public static string Entirety(QcRuleSetup qcSetup, DbUtilities dbConn, DataTable dt)
        {
            string returnStatus = "Passed";

            RuleModel rule = JsonConvert.DeserializeObject<RuleModel>(qcSetup.RuleObject);
            string indexNode = qcSetup.IndexNode;
            string dataName = rule.RuleParameters;
            string select = "SELECT DATANAME FROM pdo_qc_index ";
            string query = $" WHERE IndexNode.IsDescendantOf('{indexNode}') = 1 and DATANAME = '{dataName}'";
            DataTable children = dbConn.GetDataTable(select, query);
            if (children.Rows.Count == 0) returnStatus = "Failed";
            return returnStatus;
        }

        public static string Completeness(QcRuleSetup qcSetup, DbUtilities dbConn, DataTable dt)
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

        public static string Uniqueness(QcRuleSetup qcSetup, DbUtilities dbConn, DataTable dt)
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

        public static string ValidityRange(QcRuleSetup qcSetup, DbUtilities dbConn, DataTable dt)
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
    }
}
