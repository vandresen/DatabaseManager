using DatabaseManager.Services.Reports.Extensions;
using DatabaseManager.Services.Reports.Models;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System.Net;
using DatabaseManager.Services.Reports.Services;
using Newtonsoft.Json;

namespace DatabaseManager.Services.Reports
{
    public class Reports
    {
        private readonly ILogger<Reports> _logger;
        protected ResponseDto _response;
        private readonly IConfiguration _configuration;
        private readonly IRuleAccess _ra;
        private readonly IIndexAccess _ia;

        public Reports(ILogger<Reports> logger, IConfiguration configuration,
            IRuleAccess ra, IIndexAccess ia)
        {
            _logger = logger;
            _response = new ResponseDto();
            _configuration = configuration;
            _ra = ra;
            _ia = ia;
            SD.RuleAPIBase = _configuration.GetValue<string>("DataRuleAPI");
            SD.RuleKey = _configuration["RuleKey"];
            SD.IndexAPIBase = _configuration["IndexAPI"];
            SD.IndexKey = _configuration["IndexKey"];
        }

        [Function("GetResults")]
        public async Task<HttpResponseData> GetResults([HttpTrigger(AuthorizationLevel.Function, "get")] HttpRequestData req)
        {
            _logger.LogInformation("GetResults: Starting.");
            try
            {
                List<QcResult> qcResult = new List<QcResult>();
                string name = req.GetQuery("Name", true);
                ResponseDto dsResponse = await _ra.GetRules<ResponseDto>(name);
                if (dsResponse != null && dsResponse.IsSuccess)
                {
                    string content = Convert.ToString(dsResponse.Result);
                    List<RuleModel> rules = JsonConvert.DeserializeObject<List<RuleModel>>(content);
                    List<RuleModel> activeRules = rules.Where(x => x.Active == "Y").ToList();
                    string jsonString = JsonConvert.SerializeObject(activeRules, Formatting.Indented);
                    if (activeRules != null && activeRules.Count > 0) 
                    {
                        qcResult = JsonConvert.DeserializeObject<List<QcResult>>(jsonString);
                        foreach (QcResult qcItem in qcResult) 
                        {
                            ResponseDto iaResponse = await _ia.GetIndexFailures<ResponseDto>(name, "", qcItem.DataType, qcItem.RuleKey);
                            var indexes = JsonConvert.DeserializeObject<List<IndexDto>>(Convert.ToString(iaResponse.Result));
                            qcItem.Failures = indexes.Count;
                        }
                    }
                }
                _response.Result = qcResult;
            }
            catch (Exception ex)
            {
                _response.IsSuccess = false;
                _response.ErrorMessages
                     = new List<string>() { ex.ToString() };
                _logger.LogError($"GetResults: Error getting results for reports: {ex}");
            }

            var result = req.CreateResponse(HttpStatusCode.OK);
            await result.WriteAsJsonAsync(_response);
            return result;
        }
    }
}
