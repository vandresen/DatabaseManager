using DatabaseManager.Services.DataOps.Models;
using DatabaseManager.Services.DataOps.Services;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace DatabaseManager.Services.DataOps.ActivityFolder
{
    public class InitPredictions
    {
        private readonly IRuleAccess _ruleAccess;
        private readonly ILogger<InitPredictions> _logger;

        public InitPredictions(IRuleAccess ruleAccess, ILogger<InitPredictions> logger)
        {
            _ruleAccess = ruleAccess;
            _logger = logger;
        }

        [Function(nameof(DataOps_InitPredictions))]
        public async Task<List<QcResult>> DataOps_InitPredictions([ActivityTrigger] DataOpParameters pipe,
            FunctionContext executionContext)
        {
            _logger.LogInformation($"InitPredictions: Starting");
            if (pipe?.JsonParameters is null)
                throw new ArgumentNullException(nameof(pipe), "JsonParameters cannot be null.");

            PredictionParameters parms = JObject.Parse(pipe.JsonParameters).ToObject<PredictionParameters>()
                ?? throw new InvalidOperationException("Failed to deserialize PredictionParameters.");

            ResponseDto response = await _ruleAccess.GetRules<ResponseDto>(parms.DataConnector);
            if (response.IsSuccess)
            {
                List<QcResult> fullList = JsonConvert.DeserializeObject<List<QcResult>>(response.Result.ToString())
                    ?? throw new InvalidOperationException("Failed to deserialize QcResult list.");
                List<QcResult> qcList = fullList
                    .Where(x => x.RuleType == "Predictions")
                    .OrderBy(x => x.PredictionOrder)
                    .ToList();
                _logger.LogInformation($"DataOps_InitPredictions: Prediction list count = {qcList.Count}");
                _logger.LogInformation($"DataOps_InitPredictions: Complete");
                return qcList;
            }
            else
            {
                string error = string.Join(";", response.ErrorMessages);
                _logger.LogInformation($"DataOps_InitPredictions: Error {error}");
                throw new Exception(error);
            }
        }
    }
}
