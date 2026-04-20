using DatabaseManager.Services.DataOps.Extensions;
using DatabaseManager.Services.DataOps.Models;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace DatabaseManager.Services.DataOps.Services
{
    public class RuleAccess : BaseService, IRuleAccess
    {
        private readonly IConfiguration _configuration;

        public RuleAccess(IConfiguration configuration, IHttpClientFactory clientFactory) : base(clientFactory)
        {
            _configuration = configuration;
        }

        public async Task<T> GetRules<T>(string sourceName)
        {
            var ruleAPIBase = _configuration.GetValue<string>("DataRuleAPI");
            var ruleKey = _configuration.GetValue<string>("DataRuleKey");
            string url = ruleAPIBase.BuildFunctionUrl($"/Rules", $"Name={sourceName}", ruleKey);
            return await this.SendAsync<T>(new ApiRequest()
            {
                ApiType = SD.ApiType.GET,
                Url = url
            });
        }
    }
}
