using DatabaseManager.Services.Predictions.Core;
using DatabaseManager.Services.Predictions.Models;
using Microsoft.Extensions.Logging;
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

            PredictionRuleSetup setup = new ();
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
                        if (result.Status == "Server error") break;
                    }
                    else
                    {
                        result = (PredictionResult)info!.Invoke(null, new object[] { setup, _dp, _idxAccess });
                    }
                    if (result.Status == "Passed")
                    {
                        UpdateQCString(index, rule, result);
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

        private void UpdateQCString(IndexDto index, RuleModelDto rule, PredictionResult result)
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
        }
    }
}
