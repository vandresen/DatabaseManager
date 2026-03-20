using Azure;
using DatabaseManager.Services.Predictions.Core;
using DatabaseManager.Services.Predictions.Models;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json.Linq;
using System.Data;
using System.IO.Pipelines;
using System.Net;
using System.Reflection;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization;

namespace DatabaseManager.Services.Predictions.Services
{
    public class PredictionCore : IPrediction
    {
        private readonly ILogger<PredictionCore> _logger;
        private readonly IIndexAccess _idxAccess;
        private readonly IDatabaseAccess _dp;
        private readonly IDatabaseManagementService _dmService;
        private List<DataAccessDef> _accessDefs;
        private List<IndexDto> _newIndexes;

        private static readonly JsonSerializerOptions _jsonOptions = new()
        {
            PropertyNameCaseInsensitive = true,
            Converters = { new JsonStringEnumConverter(JsonNamingPolicy.CamelCase) }
        };

        public PredictionCore(ILogger<PredictionCore> logger, IIndexAccess idxAccess, IDatabaseAccess dp,
            IDatabaseManagementService dmService)
        {
            _logger = logger;
            _idxAccess = idxAccess;
            _dp = dp;
            _dmService = dmService;
        }

        public async Task<List<int>> ExecutePredictionAsync(List<IndexDto> indexes, RuleModelDto rule, PredictionParameters parms)
        {
            if (rule == null)
            {
                throw new InvalidOperationException($"No rule info available.");
            }
            if (rule.RuleType != "Predictions")
            {
                throw new InvalidOperationException($"Rule type '{rule.RuleType}' is not a Predictions rule.");
            }
            if (indexes  == null || indexes.Count == 0)
            {
                throw new InvalidOperationException($"No indexes found for rule '{rule.RuleName}'");
            }

            PredictionRuleSetup setup = new();

            if (rule.RuleFunction == "PredictMissingDataObjects")
            {
                var dmResponse = await _dmService.GetDataAccessDef<ResponseDto>();
                if (!dmResponse.IsSuccess)
                {
                    throw new InvalidOperationException($"Prediction method '{rule.RuleFunction}' could not get the data access definition");
                }
                if (dmResponse.Result == null)
                {
                    throw new InvalidOperationException($"Failed to get data access definition for prediction method '{rule.RuleFunction}'");
                }
                else
                {
                    setup.SourceDataAccessDef = dmResponse.Result.ToString()!;
                    _accessDefs = JsonSerializer.Deserialize<List<DataAccessDef>>(setup.SourceDataAccessDef, _jsonOptions)!;
                }
            }

            string jsonRules = JsonSerializer.Serialize(rule, _jsonOptions);
            setup.RuleObject = jsonRules;
            List<int> correctedObjects = new List<int>();
            bool externalQcMethod = rule.RuleFunction.StartsWith("http");
            List<IndexDto> correctedIndexes = new List<IndexDto>();
            _newIndexes = new List<IndexDto>();

            MethodInfo info = null;
            if (!externalQcMethod)
            {
                Type type = typeof(PredictionMethods);
                info = type.GetMethod(rule.RuleFunction)
                    ?? throw new InvalidOperationException($"Prediction method '{rule.RuleFunction}' not found.");
            }

            foreach (IndexDto index in indexes)
            {
                if (!string.IsNullOrEmpty(index.JsonDataObject))
                {
                    setup.IndexId = index.IndexId;
                    setup.IndexNode = index.TextIndexNode;
                    setup.DataObject = index.JsonDataObject;
                    PredictionResult result = new PredictionResult();
                    if (externalQcMethod)
                    {
                        result = ProcessPrediction(setup, rule);
                        if (result.Status == "Server error")
                        {
                            _logger.LogWarning($"Server error for extrenal rule");
                            break; 
                        }
                        
                    }
                    else
                    {
                        result = (PredictionResult)info!.Invoke(null, new object[] { setup, _dp, _idxAccess });
                    }
                    if (result.Status == "Passed")
                    {
                        await UpdateQCString(index, rule, result, parms);
                        correctedIndexes.Add(index);
                        correctedObjects.Add(result.IndexId); 
                    }
                    
                }
            }
            if (correctedIndexes.Count > 0)
            {
                ResponseDto updateResponse = await _idxAccess.UpdateIndexes<ResponseDto>(correctedIndexes, parms.DataConnector, parms.IndexProject, parms.AzureStorageKey);
                if (!updateResponse.IsSuccess)
                {
                    throw new InvalidOperationException($"Failed to save corrected indexes: {string.Join(", ", updateResponse.ErrorMessages)}");
                }
            }
            return correctedObjects;
        }

        private PredictionResult ProcessPrediction(PredictionRuleSetup setup, RuleModelDto rule)
        {
            PredictionResult result = new PredictionResult();
            string predictionURL = rule.RuleFunction;

            return result;
        }

        private async Task UpdateQCString(IndexDto index, RuleModelDto rule, PredictionResult result, PredictionParameters parms)
        {
            string qcStr = index.QC_String;
            string failRule = rule.FailRule + ";";
            string pCode = rule.RuleKey + ";";
            if (result.SaveType == "Delete")
            {
                qcStr = pCode;
            }
            else
            {
                qcStr = qcStr.Replace(failRule, pCode);
            }
            index.QC_String = qcStr;
            await SavePrediction(index, result, qcStr, parms);
        }

        private async Task SavePrediction(IndexDto index, PredictionResult result, string qcStr, PredictionParameters parms)
        {
            if (result.SaveType == "Update")
            {
                await UpdateAction(result, qcStr);
            }
            else if (result.SaveType == "Insert")
            {
                await InsertAction(result, parms, index);
            }
            else if (result.SaveType == "Delete")
            {
                await DeleteAction(index, result, parms);
            }
            else
            {
                _logger.LogWarning($"Save type {result.SaveType} is not supported");
            }
        }

        private async Task UpdateAction(PredictionResult result, string qcStr)
        {
            //string idxQuery = $" where INDEXID = {result.IndexId}";
            //IndexModel idxResult = await _indexData.GetIndex(result.IndexId, databaseConnectionString);
            //if (idxResult != null)
            //{
            //    string condition = $"INDEXID={result.IndexId}";
            //    var rows = indexTable.Select(condition);
            //    rows[0]["JSONDATAOBJECT"] = result.DataObject;
            //    rows[0]["QC_STRING"] = qcStr;
            //    indexTable.AcceptChanges();

            //    if (syncPredictions)
            //    {
            //        string jsonDataObject = result.DataObject;
            //        JObject dataObject = JObject.Parse(jsonDataObject);
            //        dataObject["ROW_CHANGED_BY"] = Environment.UserName;
            //        jsonDataObject = dataObject.ToString();
            //        jsonDataObject = Helpers.Common.SetJsonDataObjectDate(jsonDataObject, "ROW_CHANGED_DATE");
            //        string dataType = idxResult.DataType;
            //        try
            //        {
            //            await UpdateReferenceTables(dataType, dataObject);
            //            string storedProcedure = "dbo.spUpdate" + dataType;
            //            await _dp.SaveData(storedProcedure, new { json = jsonDataObject }, syncConnectionString);
            //        }
            //        catch (Exception ex)
            //        {
            //            string error = ex.ToString();
            //            _log.LogError($"Error updating data object: {error}");
            //            throw;
            //        }
            //    }
            //}
            //else
            //{
            //    //logger.LogWarning("Cannot find data key during update");
            //}
        }

        private async Task DeleteAction(IndexDto index, PredictionResult result, PredictionParameters parms)
        {
            await DeleteChildren(index.IndexId, index.QC_String, parms);
            index.JsonDataObject = "";

        }

        private async Task DeleteChildren(int id, string qcStr, PredictionParameters parms)
        {
            ResponseDto response = await _idxAccess.GetDescendants<ResponseDto>(id, parms.DataConnector, parms.IndexProject, parms.AzureStorageKey);
            if (!response.IsSuccess)
            {
                throw new InvalidOperationException($"Failed to children: {string.Join(", ", response.ErrorMessages)}");
            }
            var indexElement = (JsonElement)response.Result!;
            var indexes = indexElement.Deserialize<List<IndexDto>>(_jsonOptions)!;
            foreach (IndexDto index in indexes)
            {
                index.JsonDataObject = "";
                index.QC_String = qcStr;
            }
            if (indexes.Count > 0)  
            {
                ResponseDto updateResponse = await _idxAccess.UpdateIndexes<ResponseDto>(indexes, parms.DataConnector, parms.IndexProject, parms.AzureStorageKey);
                if (!updateResponse.IsSuccess)
                {
                    throw new InvalidOperationException($"Failed to save child index updates: {string.Join(", ", updateResponse.ErrorMessages)}");
                }
            }
        }

        private async Task InsertAction(PredictionResult result, PredictionParameters parms, IndexDto parentIndex)
        {
            List<IndexFileData> idxData = await GetIndexFileData(result.DataType, parms);
            IndexFileData indexdata = idxData.FirstOrDefault(s => s.DataName == result.DataType)
                ?? throw new InvalidOperationException($"No index file data found for type '{result.DataType}'");

            JsonObject dataObject = JsonNode.Parse(result.DataObject)!.AsObject();
            string dataName = dataObject[indexdata.NameAttribute]?.ToString()
                ?? throw new InvalidOperationException($"Name attribute '{indexdata.NameAttribute}' not found in data object");

            DataAccessDef dataAccessDef = _accessDefs.First(x => x.DataType == result.DataType);
            string dataKey = GetDataKey(dataObject, dataAccessDef.Keys);

            double latitude = indexdata.UseParentLocation ? (parentIndex.Latitude ?? -99999.0) : -99999.0;
            double longitude = indexdata.UseParentLocation ? (parentIndex.Longitude ?? -99999.0) : -99999.0;

            int nodeId = await GetIndexNode(result.DataType, parentIndex, parms);
            if (nodeId == 0) return;

            _newIndexes.Add(new IndexDto
            {
                Latitude = latitude,
                Longitude = longitude,
                DataType = result.DataType,
                DataName = dataName,
                DataKey = dataKey,
                JsonDataObject = result.DataObject
            });
        }

        private async Task<List<IndexFileData>> GetIndexFileData(string dataType, PredictionParameters parms)
        {
            IndexFileData indexdata = new IndexFileData();
            IndexRootJson rootJson = await GetIndexRootData(parms);
            string taxonomy = rootJson.Taxonomy;
            List<IndexFileData> idxData = GetIndexArray(taxonomy);
            indexdata = idxData.FirstOrDefault(s => s.DataName == dataType);
            return idxData;
        }

        private async Task<IndexRootJson> GetIndexRootData(PredictionParameters parms)
        {
            IndexRootJson rootJson = new IndexRootJson();
            ResponseDto response = await _idxAccess.GetRootIndex<ResponseDto>(parms.DataConnector, parms.IndexProject, parms.AzureStorageKey);
            if (!response.IsSuccess)
            {
                throw new InvalidOperationException($"Failed to get root index: {string.Join(", ", response.ErrorMessages)}");
            }
            //IndexDto idxResult = await _idxAccess.GetIndexRoot(databaseConnectionString);
            var indexElement = (JsonElement)response.Result!;
            var index = indexElement.Deserialize<IndexDto>(_jsonOptions)!;
            string jsonStringObject = index.JsonDataObject;
            rootJson = JsonSerializer.Deserialize<IndexRootJson>(jsonStringObject);
            return rootJson;
        }

        private List<IndexFileData> GetIndexArray(string taxonomy)
        {
            List<IndexFileData> idxData;
            JArray result = new JArray();
            JArray JsonIndexArray = JArray.Parse(taxonomy);
            idxData = new List<IndexFileData>();
            foreach (JToken level in JsonIndexArray)
            {
                idxData.Add(ProcessJTokens(level));
                idxData = ProcessIndexArray(JsonIndexArray, level, idxData);
            }
            return idxData;
        }

        private static IndexFileData ProcessJTokens(JToken token)
        {
            IndexFileData idxDataObject = new IndexFileData();
            idxDataObject.DataName = (string)token["DataName"];
            idxDataObject.NameAttribute = token["NameAttribute"]?.ToString();
            idxDataObject.LatitudeAttribute = token["LatitudeAttribute"]?.ToString();
            idxDataObject.LongitudeAttribute = token["LongitudeAttribute"]?.ToString();
            idxDataObject.ParentKey = token["ParentKey"]?.ToString();
            if (token["UseParentLocation"] != null) idxDataObject.UseParentLocation = (Boolean)token["UseParentLocation"];
            if (token["Arrays"] != null)
            {
                idxDataObject.Arrays = token["Arrays"];
            }
            return idxDataObject;
        }

        private List<IndexFileData> ProcessIndexArray(JArray JsonIndexArray, JToken parent, List<IndexFileData> idxData)
        {
            List<IndexFileData> result = idxData;
            if (parent["DataObjects"] != null)
            {
                foreach (JToken level in parent["DataObjects"])
                {
                    result.Add(ProcessJTokens(level));
                    result = ProcessIndexArray(JsonIndexArray, level, result);
                }
            }
            return result;
        }

        private string GetDataKey(JsonObject dataObject, string dbKeys)
        {
            string dataKey = "";
            string and = "";
            string[] keys = dbKeys.Split(',');
            foreach (string key in keys)
            {
                string attribute = key.Trim();
                string attributeValue = "'" + dataObject[attribute].ToString() + "'";
                dataKey = dataKey + and + key.Trim() + " = " + attributeValue;
                and = " AND ";
            }
            return dataKey;
        }

        private async Task<int> GetIndexNode(string dataType, IndexDto idxResult, PredictionParameters parms)
        {
            if (idxResult == null) return 0;

            int nodeid = 0;
            string nodeName = dataType + "s";

            ResponseDto response = await _idxAccess.GetDescendants<ResponseDto>(idxResult.IndexId, parms.DataConnector, parms.IndexProject, parms.AzureStorageKey);
            if (!response.IsSuccess)
            {
                throw new InvalidOperationException($"Failed to children: {string.Join(", ", response.ErrorMessages)}");
            }
            var indexElement = (JsonElement)response.Result!;
            var indexes = indexElement.Deserialize<List<IndexDto>>(_jsonOptions)!;

            var nodeIndex = indexes.FirstOrDefault(x => x.DataType == nodeName);
            if (nodeIndex != null)
            {
                nodeid = nodeIndex.IndexId;
            }

            if (nodeid == 0)
            {
                nodeIndex = new IndexDto();
                nodeIndex.Latitude = 0.0;
                nodeIndex.Longitude = 0.0;
                nodeIndex.DataType = nodeName;
                nodeIndex.DataName = nodeName;
                ResponseDto response = await _idxAccess.InsertIndex<ResponseDto>(nodeIndex, parms.DataConnector, parms.IndexProject, parms.AzureStorageKey);

                //nodeid = await _idxAccess.InsertIndex(nodeIndex, parms.DataConnector, parms.IndexProject, parms.AzureStorageKey);

                //_newIndexes.Add(new IndexDto
                //{
                //    Latitude = 0.0,
                //    Longitude = 0.0,
                //    DataType = nodeName,
                //    DataName = nodeName
                //});
            }
            
            return nodeid;
        }
    }
}
