using System.Net;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;
using DatabaseManager.Services.DataQC.Models;
using Newtonsoft.Json;
using Microsoft.Extensions.Configuration;
using DatabaseManager.Services.DataQC.Services;

namespace DatabaseManager.Services.DataQC
{
    public class DataQC
    {
        private readonly ILogger _logger;
        protected ResponseDto _response;
        private readonly IConfiguration _configuration;
        private readonly IRuleAccess _rules;
        private readonly IIndexAccess _idxAccess;
        private readonly IDataQc _dqc;

        public DataQC(ILoggerFactory loggerFactory, IConfiguration configuration,
            IRuleAccess rules, IIndexAccess idxAccess, IDataQc dqc)
        {
            _logger = loggerFactory.CreateLogger<DataQC>();
            _response = new ResponseDto();
            _configuration = configuration;
            _rules = rules;
            _idxAccess = idxAccess;
            _dqc = dqc;
            SD.RuleAPIBase = _configuration.GetValue<string>("DataRuleAPI");
            SD.RuleKey = _configuration["DataRuleKey"];
            SD.IndexAPIBase = _configuration.GetValue<string>("IndexAPI");
            SD.IndexKey = _configuration["IndexKey"];
            SD.DataConfigurationAPIBase = _configuration.GetValue<string>("DataConfigurationAPI");
            SD.DataConfigurationKey = _configuration["DataConfigurationKey"];
            SD.DataSourceAPIBase = _configuration.GetValue<string>("DataSourceAPI");
            SD.DataSourceKey = _configuration["DataSourceKey"];
        }

        [Function("DataQC")]
        public async Task<ResponseDto> Run([HttpTrigger(AuthorizationLevel.Function, "post")] HttpRequestData req)
        {
            _logger.LogInformation($"DataQC: Starting");
            try
            {
                var stringBody = await new StreamReader(req.Body).ReadToEndAsync();
                DataQCParameters parms = JsonConvert.DeserializeObject<DataQCParameters>(Convert.ToString(stringBody));
                SD.AzureStorageKey = parms.AzureStorageKey;
                _logger.LogInformation($"DataQC: ruleAPIBase is {SD.RuleAPIBase}");

                ResponseDto ruleResponse = await _rules.GetRule<ResponseDto>(parms.RuleId, parms.DataConnector);
                if (ruleResponse.IsSuccess)
                {
                    RuleModelDto rule = JsonConvert.DeserializeObject<RuleModelDto>(Convert.ToString(ruleResponse.Result));
                    _logger.LogInformation($"DataQC: Rulename is {rule.RuleName}");
                    ResponseDto idxResponse = await _idxAccess.GetIndexes<ResponseDto>(parms.DataConnector, rule.DataType);
                    if(idxResponse.IsSuccess) 
                    {
                        var indexes = JsonConvert.DeserializeObject<List<IndexDto>>(Convert.ToString(idxResponse.Result));
                        _logger.LogInformation($"DataQC: Number of indexes are {indexes.Count}");
                        List<int> failedObjects = await _dqc.QualityCheckDataType(parms, indexes, rule);
                        _response.Result = failedObjects;
                    }
                    else
                    {
                        _response.IsSuccess = false;
                        string error = $"DataQC: Could not indexes for rule {parms.RuleId}";
                        _logger.LogInformation(error);
                        _response.ErrorMessages
                         = new List<string>() { error };
                    }
                }
                else
                {
                    _response.IsSuccess = false;
                    string error = $"DataQC: Could not find rule for {parms.RuleId}";
                    _logger.LogInformation(error);
                    _response.ErrorMessages
                     = new List<string>() { error };
                }
            }
            catch (Exception ex)
            {
                _response.IsSuccess = false;
                _response.ErrorMessages
                     = new List<string>() { ex.ToString() };
                _logger.LogError($"DataQC: Error getting rules: {ex}");
            }
            

            _logger.LogInformation($"DataQC: completed");

            return _response;
        }
    }
}
