using DatabaseManager.BlazorComponents.Extensions;
using DatabaseManager.BlazorComponents.Models;
using DatabaseManager.BlazorComponents.Pages.Rules;
using DatabaseManager.Shared;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

namespace DatabaseManager.BlazorComponents.Services
{
    public class RulesService : BaseService, IRulesService
    {
        private readonly IHttpClientFactory _clientFactory;
        private readonly SingletonServices _settings;

        public RulesService(IHttpClientFactory clientFactory, SingletonServices settings) : base(clientFactory)
        {
            _clientFactory = clientFactory;
            _settings = settings;
        }

        public async Task<T> DeleteFunctionAsync<T>(string source, int id)
        {
            string url = SD.DataRuleAPIBase.BuildFunctionUrl($"/api/Function", $"Name={source}&Id={id}", SD.DataRuleKey);
            Console.WriteLine($"DeleteFunctionAsync: url = {url}");
            return await this.SendAsync<T>(new ApiRequest()
            {
                ApiType = SD.ApiType.DELETE,
                AzureStorage = _settings.AzureStorage,
                Url = url
            });
        }

        public async Task<T> DeleteRuleAsync<T>(string source, int id)
        {
            string url = SD.DataRuleAPIBase.BuildFunctionUrl($"/api/Rules", $"Name={source}&Id={id}", SD.DataRuleKey);
            Console.WriteLine($"DeleteRuleAsync: url = {url}");
            return await this.SendAsync<T>(new ApiRequest()
            {
                ApiType = SD.ApiType.DELETE,
                AzureStorage = _settings.AzureStorage,
                Url = url
            });
        }

        public Task<T> GetFunctionAsync<T>(string source, int id)
        {
            throw new NotImplementedException();
        }

        public async Task<T> GetFunctionsAsync<T>(string source)
        {
            string url = SD.DataRuleAPIBase.BuildFunctionUrl($"/api/Function", $"Name={source}", SD.DataRuleKey);
            Console.WriteLine($"GetFunctionsAsync: url = {url}");
            return await this.SendAsync<T>(new ApiRequest()
            {
                ApiType = SD.ApiType.GET,
                AzureStorage = _settings.AzureStorage,
                Url = url
            });
        }

        public async Task<T> GetPredictionAsync<T>(string predictionName)
        {
            string url = SD.DataRuleAPIBase.BuildFunctionUrl($"/api/PredictionSet", $"Name={predictionName}", SD.DataRuleKey);
            Console.WriteLine($"GetPredictionAsync: url = {url}");
            return await this.SendAsync<T>(new ApiRequest()
            {
                ApiType = SD.ApiType.GET,
                AzureStorage = _settings.AzureStorage,
                Url = url
            });
        }

        public async Task<T> GetPredictionsAsync<T>()
        {
            string url = SD.DataRuleAPIBase.BuildFunctionUrl($"/api/PredictionSet", $"", SD.DataRuleKey);
            Console.WriteLine($"GetFunctionsAsync: url = {url}");
            return await this.SendAsync<T>(new ApiRequest()
            {
                ApiType = SD.ApiType.GET,
                AzureStorage = _settings.AzureStorage,
                Url = url
            });
        }

        public async Task<T> GetRuleAsync<T>(string source, int id)
        {
            string url = SD.DataRuleAPIBase.BuildFunctionUrl($"/api/Rules", $"Name={source}&Id={id}", SD.DataRuleKey);
            Console.WriteLine($"GetRuleAsync: url = {url}");
            return await this.SendAsync<T>(new ApiRequest()
            {
                ApiType = SD.ApiType.GET,
                AzureStorage = _settings.AzureStorage,
                Url = url
            });
        }

        public async Task<T> GetRulesAsync<T>(string source)
        {
            string url = SD.DataRuleAPIBase.BuildFunctionUrl($"/api/Rules", $"Name={source}", SD.DataRuleKey);
            Console.WriteLine($"GetRulesAsync: url = {url}");
            return await this.SendAsync<T>(new ApiRequest()
            {
                ApiType = SD.ApiType.GET,
                AzureStorage = _settings.AzureStorage,
                Url = url
            });
        }

        public async Task<T> InsertFunctionAsync<T>(RuleFunctions function, string source)
        {
            string url = SD.DataRuleAPIBase.BuildFunctionUrl($"/api/Function", $"Name={source}", SD.DataRuleKey);
            Console.WriteLine($"InsertRulesAsync: url = {url}");
            return await this.SendAsync<T>(new ApiRequest()
            {
                ApiType = SD.ApiType.POST,
                AzureStorage = _settings.AzureStorage,
                Data = function,
                Url = url
            });
        }

        public async Task<T> InsertPredictionAsync<T>(PredictionSet predictionSet, string predictionName)
        {
            string url = SD.DataRuleAPIBase.BuildFunctionUrl($"/api/PredictionSet", $"", SD.DataRuleKey);
            Console.WriteLine($"InsertPredictionAsync: url = {url}");
            return await this.SendAsync<T>(new ApiRequest()
            {
                ApiType = SD.ApiType.POST,
                AzureStorage = _settings.AzureStorage,
                Data = predictionSet,
                Url = url
            });
        }

        public async Task<T> InsertRuleAsync<T>(RuleModel rule, string source)
        {
            string url = SD.DataRuleAPIBase.BuildFunctionUrl($"/api/Rules", $"Name={source}", SD.DataRuleKey);
            Console.WriteLine($"InsertRulesAsync: url = {url}");
            return await this.SendAsync<T>(new ApiRequest()
            {
                ApiType = SD.ApiType.POST,
                AzureStorage = _settings.AzureStorage,
                Data = rule,
                Url = url
            });
        }

        public Task<T> UpdateFunctionAsync<T>(RuleFunctions function, string source, int id)
        {
            throw new NotImplementedException();
        }

        public async Task<T> UpdateRuleAsync<T>(RuleModel rule, string source)
        {
            string url = SD.DataRuleAPIBase.BuildFunctionUrl($"/api/Rules", $"Name={source}", SD.DataRuleKey);
            Console.WriteLine($"UpdateRulesAsync: url = {url}");
            return await this.SendAsync<T>(new ApiRequest()
            {
                ApiType = SD.ApiType.POST,
                AzureStorage = _settings.AzureStorage,
                Data = rule,
                Url = url
            });
        }
    }
}
