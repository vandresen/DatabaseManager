using DatabaseManager.Services.Predictions.Extensions;
using DatabaseManager.Services.Predictions.Models;
using DatabaseManager.Services.Predictions.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using static Azure.Core.HttpHeader;

namespace DatabaseManager.Services.Predictions.Core
{
    static class PredictionMethods
    {
        private static Dictionary<string, int> _ageLookupCache;

        private static readonly JsonSerializerOptions _jsonOptions = new()
        {
            PropertyNameCaseInsensitive = true,
            Converters = { new JsonStringEnumConverter(JsonNamingPolicy.CamelCase) }
        };

        public static PredictionResult DeleteDataObject(PredictionRuleSetup qcSetup, IDatabaseAccess dp, IIndexAccess idxdata)
        {
            PredictionResult result = new PredictionResult();

            result.SaveType = "Delete";
            result.Status = "Passed";
            result.IndexId = qcSetup.IndexId;

            return result;
        }

        //public static PredictionResult PredictFormationOrder(PredictionRuleSetup qcSetup, DapperDataAccess dp, IndexDBAccess idxdata)
        //{
        //    if (_ageLookupCache == null)
        //    {
        //        var picks = dp.LoadData<FormationPick, dynamic>(
        //        "spGetMinMaxAllFormationPick",
        //        new { },
        //        qcSetup.DataConnector).GetAwaiter().GetResult(); ;

        //        _ageLookupCache = picks.ToDictionary(p => p.STRAT_UNIT_ID, p => p.AGE);
        //    }

        //    var result = new PredictionResult { Status = "Failed" };
        //    var dataObject = JObject.Parse(qcSetup.DataObject);
        //    var formation = Common.FixAposInStrings((string)dataObject["STRAT_UNIT_ID"]);

        //    if (!_ageLookupCache.TryGetValue(formation, out var age))
        //        return result;

        //    var rule = JsonConvert.DeserializeObject<RuleModel>(qcSetup.RuleObject);
        //    dataObject[rule.DataAttribute] = age;
        //    dataObject["REMARK"] = $"{dataObject["REMARK"]};{rule.DataAttribute} predicted by QCEngine;";

        //    result.DataObject = dataObject.ToString();
        //    result.DataType = rule.DataType;
        //    result.SaveType = "Update";
        //    result.IndexId = qcSetup.IndexId;
        //    result.Status = "Passed";

        //    return result;
        //}

        public class FormationPick
        {
            public string STRAT_UNIT_ID { get; set; }
            public int AGE { get; set; }
            public double MIN { get; set; }
            public double MAX { get; set; }
        }

        public static PredictionResult PredictDepthUsingIDW(PredictionRuleSetup qcSetup, IDatabaseAccess dp, IIndexAccess idxdata)
        {
            double? depth = null;
            PredictionResult result = new PredictionResult
            {
                Status = "Failed"
            };
            RuleModelDto rule = System.Text.Json.JsonSerializer.Deserialize<RuleModelDto>(qcSetup.RuleObject);

            string path = $"$.{rule.DataAttribute}";
            string failRule = $"%{rule.FailRule}%";
            ResponseDto response = Task.Run(() => idxdata.GetNeighbors<ResponseDto>(qcSetup.IndexId, qcSetup.DataConnector, failRule, path, qcSetup.Project))
                .GetAwaiter().GetResult();
            if (!response.IsSuccess)
            {
                return result;
            }
            var indexElement = (JsonElement)response.Result!;
            var nbs = indexElement.Deserialize<List<NeighbourIndex>>(_jsonOptions)!;
            if (nbs.Count() > 0)
            {
                depth = RuleMethodUtilities.CalculateDepthUsingIdw(nbs, qcSetup);
            }

            if (depth != null)
            {

                //JObject dataObject = JObject.Parse(qcSetup.DataObject);
                //dataObject[rule.DataAttribute] = depth;
                //string remark = dataObject["REMARK"] + $";{rule.DataAttribute} has been predicted by QCEngine;";
                //dataObject["REMARK"] = remark;
                //result.DataObject = dataObject.ToString();
                //result.DataType = rule.DataType;
                //result.SaveType = "Update";
                //result.IndexId = qcSetup.IndexId;
                //result.Status = "Passed";
            }

            return result;
        }

        //public static PredictionResult PredictDominantLithology(PredictionRuleSetup qcSetup, DapperDataAccess dp, IndexDBAccess idxdata)
        //{
        //    PredictionResult result = new PredictionResult
        //    {
        //        Status = "Failed"
        //    };

        //    JObject dataObject = JObject.Parse(qcSetup.DataObject);
        //    string uwi = dataObject["UWI"].ToString();
        //    string curveName = "GR";
        //    JToken value = dataObject.GetValue("PICK_DEPTH");
        //    double? pickDepth = value.GetNumberFromJToken();
        //    if (pickDepth == null || pickDepth == -99999.0) return result;

        //    string dataKey = $"UWI = ''{uwi}'' and CURVE_ID = ''{curveName}''";
        //    string query = $" where DataKey = '{dataKey}'";
        //    string select = idxdata.GetSelectSQL() + query;
        //    var lc = dp.ReadData<IndexModel>(select, qcSetup.DataConnector).GetAwaiter().GetResult();

        //    if (lc?.Count() != 1) return result;

        //    string logJson = lc.FirstOrDefault()?.JsonDataObject;
        //    JObject logObject = JObject.Parse(logJson);
        //    value = logObject.GetValue("NULL_REPRESENTATION");
        //    double? logNullValue = value.GetNumberFromJToken();
        //    double[] logArray = logObject["MEASURED_VALUE"].ToString().ConvertStringToArray();
        //    double[] indexArray = logObject["INDEX_VALUE"].ToString().ConvertStringToArray();

        //    if (logArray.Count() > 0)
        //    {
        //        int rowNumber = RuleMethodUtilities.GetRowNumberForPickDepth(indexArray, (double)pickDepth);

        //        double? smoothLogValue = RuleMethodUtilities.GetSmoothLogValue(logArray, (double)logNullValue, rowNumber);

        //        var rockType = LithologyInfo.GetLithology(smoothLogValue);
        //        dataObject["DOMINANT_LITHOLOGY"] = rockType.ToString();

        //        string remark = (dataObject["REMARK"]?.ToString() ?? "") + "; Pick depth has been predicted by QCEngine";
        //        dataObject["REMARK"] = remark;

        //        RuleModel rule = JsonConvert.DeserializeObject<RuleModel>(qcSetup.RuleObject);
        //        result.DataObject = dataObject.ToString();
        //        result.DataType = rule.DataType;
        //        result.SaveType = "Update";
        //        result.IndexId = qcSetup.IndexId;
        //        result.Status = "Passed";
        //    }

        //    return result;
        //}

        //public static PredictionResult PredictLogDepthAttributes(PredictionRuleSetup qcSetup, DapperDataAccess dp, IndexDBAccess idxdata)
        //{
        //    PredictionResult result = new PredictionResult
        //    {
        //        Status = "Failed"
        //    };
        //    JObject dataObject = JObject.Parse(qcSetup.DataObject);
        //    RuleModel rule = JsonConvert.DeserializeObject<RuleModel>(qcSetup.RuleObject);
        //    string jsonLog = RuleMethodUtilities.GetLogCurveDepths(qcSetup.DataObject);
        //    if (!string.IsNullOrEmpty(jsonLog))
        //    {
        //        JObject logObject = JObject.Parse(jsonLog);
        //        string attribute = rule.DataAttribute;
        //        dataObject[attribute] = logObject[attribute];
        //        string remark = dataObject["REMARK"] + $";{attribute} was calculated from curve array;";
        //        dataObject["REMARK"] = remark;
        //        result.DataObject = dataObject.ToString();
        //        result.DataType = rule.DataType;
        //        result.SaveType = "Update";
        //        result.IndexId = qcSetup.IndexId;
        //        result.Status = "Passed";
        //    }
        //    return result;
        //}

        public static PredictionResult PredictMissingDataObjects(PredictionRuleSetup qcSetup, IDatabaseAccess dp, IIndexAccess idxdata)
        {
            PredictionResult result = new PredictionResult
            {
                Status = "Failed"
            };
            RuleModelDto rule = System.Text.Json.JsonSerializer.Deserialize<RuleModelDto>(qcSetup.RuleObject);
            string rulePar = rule.RuleParameters;
            JsonDocument doc = JsonDocument.Parse(rulePar);
            string dataType = doc.RootElement.GetProperty("DataType").GetString();
            if (string.IsNullOrEmpty(rulePar))
            {
                throw new NullReferenceException("Rule parameter is null.");
            }
            List<DataAccessDef> accessDefs = JsonSerializer.Deserialize<List<DataAccessDef>>(qcSetup.SourceDataAccessDef, _jsonOptions);
            DataAccessDef accessDef = accessDefs.First(x => x.DataType == dataType);
            string table = accessDef.Select.GetTable();

            string emptyJson = RuleMethodUtilities.GetJsonForMissingDataObject(rulePar, accessDef);
            if (emptyJson == "{}")
            {
                throw new NullReferenceException("Could not create an empty json data object, maybe you are missing Datatype in parameters");
            }
            string json = RuleMethodUtilities.PopulateJsonForMissingDataObject(rulePar, emptyJson, qcSetup.DataObject);

            json = RuleMethodUtilities.AddDefaultsForMissingDataObjects(rulePar, json);

            result.DataObject = json;
            result.DataType = dataType;
            result.SaveType = "Insert";
            result.IndexId = qcSetup.IndexId;
            result.Status = "Passed";
            return result;
        }
    }
}
