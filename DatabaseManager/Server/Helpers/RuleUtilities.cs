using DatabaseManager.Server.Entities;
using DatabaseManager.Shared;
using Microsoft.AspNetCore.Hosting;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Threading.Tasks;

namespace DatabaseManager.Server.Helpers
{
    public class RuleUtilities
    {
        public static void SaveRulesFile(DbUtilities dbConn, string path)
        {
            try
            {
                string ruleFile = path + @"\DataBase\StandardRules.json";
                string jsonRule = System.IO.File.ReadAllText(ruleFile);
                JArray JsonRuleArray = JArray.Parse(jsonRule);
                foreach (JToken rule in JsonRuleArray)
                {
                    string function = rule["RuleFunction"].ToString();
                    string functionName = SaveRuleFunction(dbConn, function, path);
                    rule["RuleFunction"] = functionName;
                    string jsonDate = DateTime.Now.ToString("yyyy-MM-dd");
                    rule["CreatedDate"] = jsonDate;
                    rule["ModifiedDate"] = jsonDate;
                }
                string json = JsonRuleArray.ToString();
                dbConn.InsertDataObject(json, "Rules");
            }
            catch (Exception ex)
            {
                Exception error = new Exception($"Error saving rule: {ex}");
                throw;
            }
        }

        public static void SaveRule(DbUtilities dbConn, RuleModel rule, DataAccessDef ruleAccessDef)
        {
            rule.ModifiedBy = dbConn.GetUsername();
            rule.CreatedBy = dbConn.GetUsername();
            string jsonInsert = JsonConvert.SerializeObject(rule, Formatting.Indented);
            string json = GetRuleKey(dbConn, jsonInsert, ruleAccessDef);
            json = Common.SetJsonDataObjectDate(json, "CreatedDate");
            json = Common.SetJsonDataObjectDate(json, "ModifiedDate");
            dbConn.InsertDataObject(json, "Rules");
        }

        public static void UpdateRule(DbUtilities dbConn, RuleModel rule)
        {
            rule.ModifiedBy = dbConn.GetUsername();
            string json = JsonConvert.SerializeObject(rule, Formatting.Indented);
            json = Common.SetJsonDataObjectDate(json, "ModifiedDate");
            dbConn.UpdateDataObject(json, "Rules");
        }

        public static string SaveRuleFunction(DbUtilities dbConn, string ruleFunction, string path)
        {
            int startFunctionName = ruleFunction.IndexOf(@"/api/") + 5;
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

            List<DataAccessDef> dataDefs = new List<DataAccessDef>();
            string jsonFile = path + @"\DataBase\PPDMDataAccess.json";
            string json = System.IO.File.ReadAllText(jsonFile);
            dataDefs = JsonConvert.DeserializeObject<List<DataAccessDef>>(json);
            DataAccessDef dataDef = dataDefs.First(x => x.DataType == "Functions");
            Dictionary<string, string> function = new Dictionary<string, string>();
            string[] attributes = Common.GetAttributes(dataDef.Select);
            foreach (string attribute in attributes)
            {
                function.Add(attribute.Trim(), "");
            }
            function["FunctionName"] = functionname;
            function["FunctionUrl"] = functionUrl;
            function["FunctionKey"] = functionKey;
            string jsonInsert = JsonConvert.SerializeObject(function, Formatting.Indented);
            dbConn.InsertDataObject(jsonInsert, "Functions");

            return functionname;
        }

        private static string GetRuleKey(DbUtilities dbConn, string jsonInput, DataAccessDef ruleAccessDef)
        {
            JObject dataObject = JObject.Parse(jsonInput);
            string category = dataObject["RuleType"].ToString();
            string select = ruleAccessDef.Select;
            string query = $" where RuleType = '{category}' order by keynumber desc";
            DataTable dt = dbConn.GetDataTable(select, query);
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
