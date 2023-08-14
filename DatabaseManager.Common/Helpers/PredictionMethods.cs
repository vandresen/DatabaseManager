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
using System.Runtime.Intrinsics.Arm;
using System.Text;
using System.Threading.Tasks;

namespace DatabaseManager.Common.Helpers
{
    static class PredictionMethods
    {
        public static PredictionResult DeleteDataObject(QcRuleSetup qcSetup, DbUtilities dbConn, IndexDBAccess idxdata)
        {
            PredictionResult result = new PredictionResult();

            result.SaveType = "Delete";
            result.Status = "Passed";
            result.IndexId = qcSetup.IndexId;

            return result;
        }

        public static PredictionResult PredictFormationOrder(QcRuleSetup qcSetup, DbUtilities dbConn, IndexDBAccess idxdata)
        {
            List<StratUnits> inv = new List<StratUnits>();
            PredictionResult result = new PredictionResult
            {
                Status = "Failed"
            };
            string formation;
            string tempTable = "#MinMaxAllFormationPick";

            DataTable idx = new DataTable();

            try
            {
                string select = "select * from #MinMaxAllFormationPick";
                string query = "";
                idx = dbConn.GetDataTable(select, query);
            }
            catch (Exception ex)
            {
                if (ex.InnerException.Message.Contains("Invalid object name"))
                {
                    string select = $"EXEC spGetMinMaxAllFormationPick";
                    string query = "";
                    idx = dbConn.GetDataTable(select, query);
                    string SQLCreateTempTable = Common.GetCreateSQLFromDataTable(tempTable, idx);
                    dbConn.SQLExecute(SQLCreateTempTable);
                    dbConn.BulkCopy(idx, tempTable);
                }
                else
                {
                    throw;
                }
            }

            JObject dataObject = JObject.Parse(qcSetup.DataObject);
            formation = dataObject["STRAT_UNIT_ID"].ToString();
            string tmpFormation = Common.FixAposInStrings(formation);
            string condition = $"STRAT_UNIT_ID = '{tmpFormation}'";
            var rows = idx.Select(condition);
            if (rows.Length == 1)
            {
                RuleModel rule = JsonConvert.DeserializeObject<RuleModel>(qcSetup.RuleObject);
                int age = Convert.ToInt32(rows[0]["AGE"]);
                dataObject[rule.DataAttribute] = age;
                string remark = dataObject["REMARK"] + $";{rule.DataAttribute} has been predicted by QCEngine;";
                dataObject["REMARK"] = remark;
                result.DataObject = dataObject.ToString();
                result.DataType = rule.DataType;
                result.SaveType = "Update";
                result.IndexId = qcSetup.IndexId;
                result.Status = "Passed";
            }
            else if (rows.Length > 1)
            {
                throw new Exception("PredictFormationOrder: Multiple occurences of formation not allowed");
            }

            return result;
        }

        public static PredictionResult PredictDepthUsingIDW(QcRuleSetup qcSetup, DbUtilities dbConn, IndexDBAccess idxdata)
        {
            double? depth = null;
            PredictionResult result = new PredictionResult
            {
                Status = "Failed"
            };
            DataTable nb = RuleMethodUtilities.GetNeighbors(qcSetup);
            if (nb != null)
            {
                depth = RuleMethodUtilities.CalculateDepthUsingIdw(nb, qcSetup);
            }

            if (depth != null)
            {
                RuleModel rule = JsonConvert.DeserializeObject<RuleModel>(qcSetup.RuleObject);
                JObject dataObject = JObject.Parse(qcSetup.DataObject);
                dataObject[rule.DataAttribute] = depth;
                string remark = dataObject["REMARK"] + $";{rule.DataAttribute} has been predicted by QCEngine;";
                dataObject["REMARK"] = remark;
                result.DataObject = dataObject.ToString();
                result.DataType = rule.DataType;
                result.SaveType = "Update";
                result.IndexId = qcSetup.IndexId;
                result.Status = "Passed";
            }

            return result;
        }

        public static PredictionResult PredictDominantLithology(QcRuleSetup qcSetup, DbUtilities dbConn, IndexDBAccess idxdata)
        {
            PredictionResult result = new PredictionResult
            {
                Status = "Failed"
            };

            JObject dataObject = JObject.Parse(qcSetup.DataObject);
            string uwi = dataObject["UWI"].ToString();
            string curveName = "GR";
            JToken value = dataObject.GetValue("PICK_DEPTH");
            double? pickDepth = value.GetNumberFromJToken();
            if (pickDepth == null || pickDepth == -99999.0)
            {
                result.Status = "Failed";
            }
            else
            {
                IndexModel idxResult = Task.Run(() => idxdata.GetIndexRoot(qcSetup.DataConnector)).GetAwaiter().GetResult();
                if (idxResult != null)
                {
                    string jsonStringObject = idxResult.JsonDataObject;
                    IndexRootJson rootJson = JsonConvert.DeserializeObject<IndexRootJson>(jsonStringObject);
                    ConnectParameters source = JsonConvert.DeserializeObject<ConnectParameters>(rootJson.Source);
                    List<DataAccessDef> accessDefs = JsonConvert.DeserializeObject<List<DataAccessDef>>(source.DataAccessDefinition);

                    DataAccessDef logType = accessDefs.First(x => x.DataType == "Log");
                    string select = logType.Select;
                    string query = $" where CURVE_ID = '{curveName}' and UWI = '{uwi}'";
                    DataTable lc = dbConn.GetDataTable(select, query);
                    if (lc.Rows.Count == 1)
                    {
                        double logNullValue = Common.GetDataRowNumber(lc.Rows[0], "NULL_REPRESENTATION");

                        DataAccessDef logCurvedType = accessDefs.First(x => x.DataType == "LogCurve");
                        select = logCurvedType.Select;
                        query = $" where CURVE_ID = '{curveName}' and UWI = '{uwi}'";
                        DataTable lg = dbConn.GetDataTable(select, query);
                        DataTable sortedCurve = RuleMethodUtilities.GetSortedLogCurve(lg, uwi);

                        if (sortedCurve.Rows.Count > 0)
                        {
                            int rowNumber = RuleMethodUtilities.GetRowNumberForPickDepth(sortedCurve, (double)pickDepth);

                            double? smoothLogValue = RuleMethodUtilities.GetSmoothLogValue(sortedCurve, logNullValue, rowNumber);

                            string rock;
                            if (smoothLogValue >= 0 & smoothLogValue < 10)
                            {
                                rock = "Salt";
                            }
                            if (smoothLogValue >= 10 & smoothLogValue < 20)
                            {
                                rock = "Limestone";
                            }
                            else if (smoothLogValue >= 20 & smoothLogValue < 55)
                            {
                                rock = "Sandstone";
                            }
                            else if (smoothLogValue >= 55 & smoothLogValue < 150)
                            {
                                rock = "Shale";
                            }
                            else
                            {
                                rock = "Unknown";
                            }

                            dataObject["DOMINANT_LITHOLOGY"] = rock;
                            string remark = dataObject["REMARK"] + $";Pick depth has been predicted by QCEngine";
                            dataObject["REMARK"] = remark;

                            RuleModel rule = JsonConvert.DeserializeObject<RuleModel>(qcSetup.RuleObject);
                            result.DataObject = dataObject.ToString();
                            result.DataType = rule.DataType;
                            result.SaveType = "Update";
                            result.IndexId = qcSetup.IndexId;
                            result.Status = "Passed";
                        }
                    }
                }
            }

            return result;
        }

        public static PredictionResult PredictLogDepthAttributes(QcRuleSetup qcSetup, DbUtilities dbConn, IndexDBAccess idxdata)
        {
            PredictionResult result = new PredictionResult
            {
                Status = "Failed"
            };
            JObject dataObject = JObject.Parse(qcSetup.DataObject);
            RuleModel rule = JsonConvert.DeserializeObject<RuleModel>(qcSetup.RuleObject);
            string jsonLog = RuleMethodUtilities.GetLogCurveDepths(qcSetup.DataObject);
            if (!string.IsNullOrEmpty(jsonLog))
            {
                JObject logObject = JObject.Parse(jsonLog);
                string attribute = rule.DataAttribute;
                dataObject[attribute] = logObject[attribute];
                string remark = dataObject["REMARK"] + $";{attribute} was calculated from curve array;";
                dataObject["REMARK"] = remark;
                result.DataObject = dataObject.ToString();
                result.DataType = rule.DataType;
                result.SaveType = "Update";
                result.IndexId = qcSetup.IndexId;
                result.Status = "Passed";
            }
            return result;
        }

        public static PredictionResult PredictMissingDataObjects(QcRuleSetup qcSetup, DbUtilities dbConn, IndexDBAccess idxdata)
        {
            PredictionResult result = new PredictionResult
            {
                Status = "Failed"
            };
            RuleModel rule = JsonConvert.DeserializeObject<RuleModel>(qcSetup.RuleObject);
            string rulePar = rule.RuleParameters;
            JObject ruleParObject = JObject.Parse(rulePar);
            string dataType = ruleParObject["DataType"].ToString();
            if (string.IsNullOrEmpty(rulePar))
            {
                throw new NullReferenceException("Rule parameter is null.");
            }

            DataAccessDef accessDef = RuleMethodUtilities.GetDataAccessDefintionFromRoot(idxdata, qcSetup.DataConnector, dataType);
            string table = Common.GetTable(accessDef.Select);
            IDapperDataAccess dp;
            ISystemData systemData;
            dp = new DapperDataAccess();
            systemData = new SystemDBData(dp);
            IEnumerable<TableSchema> attributeProperties = (IEnumerable<TableSchema>)Task.Run(() => systemData.GetColumnInfo(qcSetup.DataConnector, table)).GetAwaiter().GetResult();

            string emptyJson = RuleMethodUtilities.GetJsonForMissingDataObject(rulePar, accessDef, attributeProperties);
            if (emptyJson == "Error")
            {
                throw new NullReferenceException("Could not create an empty json data object, maybe you are missing Datatype in parameters");
            }
            string json = RuleMethodUtilities.PopulateJsonForMissingDataObject(rulePar, emptyJson, qcSetup.DataObject);
            if (json == "Error")
            {
                throw new NullReferenceException("Could not create an json data object, problems with keys in parameters");
            }
            json = RuleMethodUtilities.AddDefaultsForMissingDataObjects(rulePar, json);
            if (json == "Error")
            {
                throw new NullReferenceException("Could not create an json data object, problems with defaults in parameters");
            }
            result.DataObject = json;
            result.DataType = dataType;
            result.SaveType = "Insert";
            result.IndexId = qcSetup.IndexId;
            result.Status = "Passed";
            return result;
        }
    }
}
