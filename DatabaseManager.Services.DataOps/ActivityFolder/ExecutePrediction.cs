using DatabaseManager.Services.DataOps.Models;
using DatabaseManager.Services.DataOps.Services;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json.Linq;

namespace DatabaseManager.Services.DataOps.ActivityFolder
{
    public class ExecutePrediction
    {
        private readonly IPredictionService _predictionService;
        private readonly ILogger<ExecutePrediction> _logger;

        public ExecutePrediction(IPredictionService predictionService, ILogger<ExecutePrediction> logger)
        {
            _predictionService = predictionService ?? throw new ArgumentNullException(nameof(predictionService));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        [Function(nameof(DataOps_Prediction))]
        public async Task<string> DataOps_Prediction([ActivityTrigger] DataOpParameters pipe,
            FunctionContext executionContext)
        {
            _logger.LogInformation($"Prediction: Starting");
            if (pipe?.JsonParameters is null)
                throw new ArgumentNullException(nameof(pipe), "JsonParameters cannot be null.");

            PredictionParameters parms = JObject.Parse(pipe.JsonParameters).ToObject<PredictionParameters>()
                ?? throw new InvalidOperationException("Failed to deserialize PredictionParameters.");

            parms.AzureStorageKey = pipe.StorageAccount;
            _logger.LogInformation($"Prediction: Processing prediction id {parms.PredictionId}");

            ResponseDto response = await _predictionService.ProcessPrediction<ResponseDto>(parms);

            if (!response.IsSuccess)
            {
                string error = string.Join(";", response.ErrorMessages);

                if (error.Contains("no indexes found", StringComparison.OrdinalIgnoreCase))
                {
                    _logger.LogWarning("DataOps_Prediction: No indexes found for prediction {PredictionId}, skipping.", parms.PredictionId);
                    return $"Skipped Prediction Rule {pipe.Id} - No indexes found";
                }

                _logger.LogError("DataOps_Prediction: Error {Error}", error);
                throw new InvalidOperationException($"Prediction failed: {error}");
            }

            _logger.LogInformation("Prediction: Complete");
            return $"Completed Prediction Rule {pipe.Id}";
        }
    }
}
