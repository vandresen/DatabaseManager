using DatabaseManager.Services.DataOps.Extensions;
using Microsoft.Extensions.Configuration;
using DatabaseManager.Services.DataOps.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace DatabaseManager.Services.DataOps.Services
{
    public class RuleAccess : BaseService, IRuleAccess
    {
        private readonly IConfiguration _configuration;
        private readonly IHttpClientFactory _clientFactory;

        public RuleAccess(IConfiguration configuration,
            IHttpClientFactory clientFactory, ILoggerFactory loggerFactory) : base(clientFactory)
        {
            _configuration = configuration;
            _clientFactory = clientFactory;
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
