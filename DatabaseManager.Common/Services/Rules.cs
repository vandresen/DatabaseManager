using DatabaseManager.Common.Extensions;
using DatabaseManager.Shared;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace DatabaseManager.Common.Services
{
    public class Rules : IRules
    {
        private readonly IHttpService httpService;
        private string url;
        private string baseUrl;
        private readonly string apiKey;

        public Rules(IHttpService httpService, SingletonService settings)
        {
            this.httpService = httpService;
            baseUrl = settings.BaseUrl;
            apiKey = settings.ApiKey;
        }

        public Task DeletePrediction(string predictionName)
        {
            throw new NotImplementedException();
        }

        public Task DeleteRule(string source, int id)
        {
            throw new NotImplementedException();
        }

        public Task<List<RuleModel>> GetPrediction(string predictionName)
        {
            throw new NotImplementedException();
        }

        public Task<List<PredictionSet>> GetPredictions()
        {
            throw new NotImplementedException();
        }

        public async Task<RuleModel> GetRule(string source, int id)
        {
            if (string.IsNullOrEmpty(baseUrl)) url = $"api/rules/{source}/{id}";
            else url = baseUrl.BuildFunctionUrl("GetDataRule", $"name={source}&id={id}", apiKey);
            var response = await httpService.Get<RuleModel>(url);
            if (!response.Success)
            {
                throw new ApplicationException(await response.GetBody());
            }
            return response.Response;
        }

        public Task<RuleInfo> GetRuleInfo(string source)
        {
            throw new NotImplementedException();
        }

        public Task<List<RuleModel>> GetRules(string source)
        {
            throw new NotImplementedException();
        }

        public Task InsertPrediction(PredictionSet predictionSet, string predictionName)
        {
            throw new NotImplementedException();
        }

        public Task InsertRule(RuleModel rule, string source)
        {
            throw new NotImplementedException();
        }

        public Task UpdateRule(RuleModel rule, string source, int id)
        {
            throw new NotImplementedException();
        }
    }
}
