﻿using DatabaseManager.Services.Reports.Extensions;
using DatabaseManager.Services.Reports.Models;

namespace DatabaseManager.Services.Reports.Services
{
    public class RuleAccess : BaseService, IRuleAccess
    {
        private readonly IHttpClientFactory _clientFactory;

        public RuleAccess(IHttpClientFactory clientFactory) : base(clientFactory)
        {
            _clientFactory = clientFactory;
        }

        public async Task<T> GetRule<T>(string sourceName, int id)
        {
            string url = SD.RuleAPIBase.BuildFunctionUrl($"/Rules", $"Name={sourceName}&Id={id}", SD.RuleKey);
            Console.WriteLine($"GetRule: url = {url}");
            return await this.SendAsync<T>(new ApiRequest()
            {
                ApiType = SD.ApiType.GET,
                Url = url
            });
        }

        public async Task<T> GetRules<T>(string sourceName)
        {
            string url = SD.RuleAPIBase.BuildFunctionUrl($"/Rules", $"Name={sourceName}", SD.RuleKey);
            Console.WriteLine($"GetRules: url = {url}");
            return await this.SendAsync<T>(new ApiRequest()
            {
                ApiType = SD.ApiType.GET,
                Url = url
            });
        }
    }
}
