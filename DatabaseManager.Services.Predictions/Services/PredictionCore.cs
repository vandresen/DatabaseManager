using Azure;
using DatabaseManager.Services.Predictions.Core;
using DatabaseManager.Services.Predictions.Models;
using Microsoft.Extensions.Logging;
using System.Data;
using System.IO.Pipelines;
using System.Net;
using System.Reflection;
using System.Text.Json;

namespace DatabaseManager.Services.Predictions.Services
{
    public class PredictionCore : IPrediction
    {
        private readonly ILogger<PredictionCore> _logger;
        private readonly IIndexAccess _idxAccess;
        private readonly IDatabaseAccess _dp;

        private static readonly JsonSerializerOptions _jsonOptions = new()
        {
            PropertyNameCaseInsensitive = true
        };

        public PredictionCore(ILogger<PredictionCore> logger, IIndexAccess idxAccess, IDatabaseAccess dp)
        {
            _logger = logger;
            _idxAccess = idxAccess;
            _dp = dp;
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
                var idxRootResponse = await _idxAccess.GetIndex<ResponseDto>(1, parms.IndexProject, parms.AzureStorageKey);
                if (!idxRootResponse.IsSuccess)
                {
                    throw new InvalidOperationException($"Prediction method '{rule.RuleFunction}' could not get the root index object");
                }
                JsonElement element = (JsonElement)idxRootResponse.Result;
                IndexDto idx = element.Deserialize<IndexDto>(new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                });
                if (!string.IsNullOrEmpty(idx.JsonDataObject))
                {
                    IndexRootJson rootJson = JsonSerializer.Deserialize<IndexRootJson>(idx.JsonDataObject);
                    setup.SourceDataAccessDef = rootJson.Source;
                }
                else
                {
                    throw new InvalidOperationException($"Failed to get data access definition for prediction method '{rule.RuleFunction}'");
                }
            }

            
            string jsonRules = JsonSerializer.Serialize(rule, _jsonOptions);
            setup.RuleObject = jsonRules;
            List<int> correctedObjects = new List<int>();
            bool externalQcMethod = rule.RuleFunction.StartsWith("http");
            List<IndexDto> correctedIndexes = new List<IndexDto>();

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
                await InsertAction(result);
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

        private async Task InsertAction(PredictionResult result)
        {
            //await InsertMissingObjectToIndex(result);
            //if (syncPredictions)
            //{
            //    await InsertMissingObjectToDatabase(result);
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
    }
}
