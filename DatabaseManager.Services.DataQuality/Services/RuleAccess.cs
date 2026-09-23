using DatabaseManager.Services.DataQuality.Extensions;
using DatabaseManager.Services.DataQuality.Models;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace DatabaseManager.Services.DataQuality.Services
{
    public class RuleAccess : BaseService, IRuleAccess
    {
        private readonly ILogger<RuleAccess> _logger;
        private readonly string _ruleApiBase;

        public RuleAccess(IHttpClientFactory clientFactory, ILogger<RuleAccess> logger, IConfiguration configuration) : base(clientFactory)
        {
            _logger = logger;

            _ruleApiBase = configuration["DataRuleAPI"]
                ?? throw new InvalidOperationException("DataRuleAPI is not configured");
        }
        public async Task<T> GetRule<T>(int id, string sourceName)
        {
            string url = _ruleApiBase.BuildFunctionUrl($"/Rule", $"Name={sourceName}&Id={id}", "");
            _logger.LogInformation("Retrieving rule {RuleId} from {Source}", id, sourceName);
            return await this.SendAsync<T>(new ApiRequest()
            {
                ApiType = SD.ApiType.GET,
                Url = url
            });
        }
    }
}
