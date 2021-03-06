﻿using DatabaseManager.Server.Entities;
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
        public static void SaveRulesFile(DbUtilities dbConn, string path, List<DataAccessDef> accessDefs)
        {
            try
            {
                List<RuleFunctions> ruleFunctions = new List<RuleFunctions>();
                DataAccessDef accessDef = accessDefs.First(x => x.DataType == "Functions");
                string sql = accessDef.Select;
                string query = "";
                DataTable ft = dbConn.GetDataTable(sql, query);

                string ruleFile = path + @"\DataBase\StandardRules.json";
                string jsonRule = System.IO.File.ReadAllText(ruleFile);
                JArray JsonRuleArray = JArray.Parse(jsonRule);
                foreach (JToken rule in JsonRuleArray)
                {
                    string function = rule["RuleFunction"].ToString();
                    string functionType = "";
                    if (rule["RuleType"].ToString() == "Validity") functionType = "V";
                    if (rule["RuleType"].ToString() == "Predictions") functionType = "P";
                    RuleFunctions ruleFunction = SaveRuleFunction(dbConn, function, path, functionType);
                    string functionName = ruleFunction.FunctionName;
                    string functionQuery = $"FunctionName = '{functionName}'";
                    DataRow[] rows = ft.Select(functionQuery);
                    if (rows.Length == 0)
                    {
                        RuleFunctions result = ruleFunctions.FirstOrDefault(s => s.FunctionName == functionName);
                        if (result == null) ruleFunctions.Add(ruleFunction);
                    }
                    
                    rule["RuleFunction"] = ruleFunction.FunctionName;
                    string jsonDate = DateTime.Now.ToString("yyyy-MM-dd");
                    rule["CreatedDate"] = jsonDate;
                    rule["ModifiedDate"] = jsonDate;
                }
                string jsonFunctions = JsonConvert.SerializeObject(ruleFunctions, Formatting.Indented);
                dbConn.InsertDataObject(jsonFunctions, "Functions");

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

        public static RuleFunctions SaveRuleFunction(DbUtilities dbConn, string ruleFunction, string path, string functionType)
        {
            RuleFunctions rf = new RuleFunctions();

            int startFunctionName = ruleFunction.IndexOf(@"/api/");
            if (startFunctionName == -1) startFunctionName = 0;
            else startFunctionName = startFunctionName + 5;

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

            rf.FunctionName = functionname;
            rf.FunctionUrl = functionUrl;
            rf.FunctionKey = functionKey;
            rf.FunctionType = functionType;
            return rf;
        }

        public static RuleModel GetRule(DbUtilities dbConn, int id, List<DataAccessDef> accessDefs)
        {
            List<RuleModel> rules = new List<RuleModel>();
            DataAccessDef ruleAccessDef = accessDefs.First(x => x.DataType == "Rules");
            string sql = ruleAccessDef.Select;
            string query = $" where Id = {id}";
            DataTable dt = dbConn.GetDataTable(sql, query);
            string jsonString = JsonConvert.SerializeObject(dt);
            rules = JsonConvert.DeserializeObject<List<RuleModel>>(jsonString);
            RuleModel rule = rules.First();

            DataAccessDef functionAccessDef = accessDefs.First(x => x.DataType == "Functions");
            sql = functionAccessDef.Select;
            query = $" where FunctionName = '{rule.RuleFunction}'";
            dt = dbConn.GetDataTable(sql, query);

            string functionURL = dt.Rows[0]["FunctionUrl"].ToString();
            string functionKey = dt.Rows[0]["FunctionKey"].ToString();
            if (!string.IsNullOrEmpty(functionKey)) functionKey = "?code=" + functionKey;
            rule.RuleFunction = functionURL + functionKey;
            return rule;
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
