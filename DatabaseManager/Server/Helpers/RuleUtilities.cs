using DatabaseManager.Server.Entities;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
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
    }
}
