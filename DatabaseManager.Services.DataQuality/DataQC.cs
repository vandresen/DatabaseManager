using Azure;
using DatabaseManager.Services.DataQuality.Models;
using DatabaseManager.Services.DataQuality.Services;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System.Net;
using System.Text.Json;

namespace DatabaseManager.Services.DataQuality;

public class DataQC
{
    private readonly ILogger<DataQC> _logger;
    private readonly IConfiguration _configuration;
    private readonly IRuleAccess _rules;
    private readonly IIndexAccess _idxAccess;
    private readonly IDataQc _dataQc;

    public DataQC(ILogger<DataQC> logger, IConfiguration configuration, IRuleAccess rules,
        IIndexAccess idxAccess, IDataQc dataQc)
    {
        _logger = logger;
        _configuration = configuration;
        _rules = rules;
        _idxAccess = idxAccess;
        _dataQc = dataQc;
    }

    [Function("DataQC")]
    public async Task<HttpResponseData> Run([HttpTrigger(AuthorizationLevel.Function, "post")] HttpRequestData req)
    {
        _logger.LogInformation($"DataQC: Starting");
        var response = req.CreateResponse();
        var result = new ResponseDto();
        try
        {
            var body = await new StreamReader(req.Body).ReadToEndAsync();

            if (string.IsNullOrWhiteSpace(body))
            {
                response.StatusCode = HttpStatusCode.BadRequest;
                result.IsSuccess = false;
                result.ErrorMessages.Add("Request body is empty");
                await response.WriteAsJsonAsync(result);
                return response;
            }

            var parms = JsonSerializer.Deserialize<DataQCParameters>(
                body,
                new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                });

            if (parms is null)
            {
                response.StatusCode = HttpStatusCode.BadRequest;
                result.IsSuccess = false;
                result.ErrorMessages.Add("Invalid DataQCParameters payload");
                await response.WriteAsJsonAsync(result);
                return response;
            }

            ResponseDto ruleResponse = await _rules.GetRule<ResponseDto>(parms.RuleId, parms.DataConnector);
            if (!ruleResponse.IsSuccess)
            {
                response.StatusCode = HttpStatusCode.BadRequest;
                await response.WriteAsJsonAsync(ruleResponse);
                return response;
            }

            var ruleElement = (JsonElement)ruleResponse.Result!;
            var rule = ruleElement.Deserialize<RuleModelDto>(
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true })!;

            _logger.LogInformation("DataQC: RuleName={RuleName}", rule.RuleName);

            var idxResponse = await _idxAccess.GetIndexes<ResponseDto>(parms.DataConnector, parms.IndexProject, rule.DataType);
            if (!idxResponse.IsSuccess)
            {
                response.StatusCode = HttpStatusCode.BadRequest;
                await response.WriteAsJsonAsync(idxResponse);
                return response;
            }
            var indexElement = (JsonElement)idxResponse.Result!;
            var indexes = indexElement.Deserialize<List<IndexDto>>(
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true })!;
            _logger.LogInformation($"DataQC: Number of indexes are {indexes.Count}");

            DataQcResult failedObjects = await _dataQc.QualityCheckDataAsync(parms, indexes, rule);

            response.StatusCode = HttpStatusCode.OK;
            result.Result = failedObjects.FailedIndexes;
        }
        catch (JsonException ex)
        {
            _logger.LogWarning(ex, "DataQC: JSON deserialization error");

            response.StatusCode = HttpStatusCode.BadRequest;
            result.IsSuccess = false;
            result.ErrorMessages.Add("Malformed JSON payload");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "DataQC: Unhandled exception");
            response.StatusCode = HttpStatusCode.InternalServerError;
            result.IsSuccess = false;
            result.ErrorMessages = [ex.Message];
        }

        await response.WriteAsJsonAsync(result);
        return response;
    }
}