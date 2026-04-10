using DatabaseManager.Services.Predictions.Models;
using DatabaseManager.Services.Predictions.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;
using System.Net;
using System.Text.Json;

namespace DatabaseManager.Services.Predictions;

public class Predictions
{
    private readonly ILogger<Predictions> _logger;
    private readonly IRuleAccess _rules;
    private readonly IIndexAccess _idxAccess;
    private readonly IPrediction _predictions;

    private static readonly JsonSerializerOptions _jsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    public Predictions(ILogger<Predictions> logger, IRuleAccess rules, IIndexAccess idxAccess, IPrediction predictions)
    {
        _logger = logger;
        _rules = rules;
        _idxAccess = idxAccess;
        _predictions = predictions;
    }

    [Function("Predictions")]
    public async Task<HttpResponseData> Run([HttpTrigger(AuthorizationLevel.Function, "post")] HttpRequestData req)
    {
        _logger.LogInformation($"Predictions: Starting");
        var response = req.CreateResponse();
        var result = new ResponseDto();
        try
        {
            using var reader = new StreamReader(req.Body);
            var body = await reader.ReadToEndAsync();
            if (string.IsNullOrWhiteSpace(body))
            {
                response.StatusCode = HttpStatusCode.BadRequest;
                result.IsSuccess = false;
                result.ErrorMessages.Add("Predictions: Request body is empty");
                await response.WriteAsJsonAsync(result);
                return response;
            }

            var parms = JsonSerializer.Deserialize<PredictionParameters>(body, _jsonOptions);

            if (parms is null)
            {
                response.StatusCode = HttpStatusCode.BadRequest;
                result.IsSuccess = false;
                result.ErrorMessages.Add("Invalid PredictionParameters payload");
                await response.WriteAsJsonAsync(result);
                return response;
            }

            ResponseDto ruleResponse = await _rules.GetRuleAndFunction<ResponseDto>(parms.PredictionId, parms.DataConnector);
            if (!ruleResponse.IsSuccess)
            {
                response.StatusCode = HttpStatusCode.BadRequest;
                await response.WriteAsJsonAsync(ruleResponse);
                return response;
            }

            var ruleElement = (JsonElement)ruleResponse.Result!;
            var rule = ruleElement.Deserialize<RuleModelDto>(_jsonOptions)!;

            _logger.LogInformation("Predictions: RuleName={RuleName}", rule.RuleName);

            var idxResponse = await _idxAccess.GetIndexes<ResponseDto>(parms.DataConnector, parms.IndexProject, rule.DataType, parms.AzureStorageKey);
            if (!idxResponse.IsSuccess)
            {
                response.StatusCode = HttpStatusCode.BadRequest;
                await response.WriteAsJsonAsync(idxResponse);
                return response;
            }
            var json = idxResponse.Result!.ToString()!;
            var indexes = JsonSerializer.Deserialize<List<IndexDto>>(json, _jsonOptions)!;
            _logger.LogInformation($"Predictions: Number of indexes are {indexes.Count}");
            var failedIndexes = indexes.Where(i => !string.IsNullOrEmpty(i.QC_String) && i.QC_String.Contains(rule.FailRule + ";")).ToList();

            _logger.LogInformation("Predictions: Number of failed indexes are {IndexCount}", failedIndexes.Count);

            List<int> correctedObjects = await _predictions.ExecutePredictionAsync(failedIndexes, rule, parms);
            _logger.LogInformation("Predictions: Corrected {Count} objects", correctedObjects.Count);

            response.StatusCode = HttpStatusCode.OK;
            result.Result = correctedObjects;
        }
        catch (JsonException ex)
        {
            _logger.LogWarning(ex, "Predictions: JSON deserialization error");

            response.StatusCode = HttpStatusCode.BadRequest;
            result.IsSuccess = false;
            result.ErrorMessages.Add("Malformed JSON payload");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Predictions: Unhandled exception");
            response.StatusCode = HttpStatusCode.InternalServerError;
            result.IsSuccess = false;
            result.ErrorMessages = [ex.Message];
        }
        await response.WriteAsJsonAsync(result);
        return response;
    }
}