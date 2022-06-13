using DatabaseManager.BlazorComponents.Extensions;
using DatabaseManager.Shared;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DatabaseManager.BlazorComponents.Services
{
    public class Rules : IRules
    {
        private readonly IHttpService httpService;
        private string url;
        private string baseUrl;
        private readonly string apiKey;

        public Rules(IHttpService httpService, SingletonServices settings)
        {
            this.httpService = httpService;
            baseUrl = settings.BaseUrl;
            apiKey = settings.ApiKey;
        }

        public async Task DeletePrediction(string predictionName)
        {
            if (string.IsNullOrEmpty(baseUrl)) url = $"api/rules/RuleFile/{predictionName}";
            else url = baseUrl.BuildFunctionUrl("DeletePredictionSet", $"name={predictionName}", apiKey);
            var response = await httpService.Delete(url);
            if (!response.Success)
            {
                throw new ApplicationException(await response.GetBody());
            }
        }

        public async Task DeleteRule(string source, int id)
        {
            if (string.IsNullOrEmpty(baseUrl)) url = $"api/rules/{source}/{id}";
            else url = baseUrl.BuildFunctionUrl("DeleteDataRule", $"name={source}&id={id}", apiKey);
            var response = await httpService.Delete(url);
            if (!response.Success)
            {
                throw new ApplicationException(await response.GetBody());
            }
        }

        public async Task<List<RuleModel>> GetPrediction(string predictionName)
        {
            if (string.IsNullOrEmpty(baseUrl)) url = $"api/rules/RuleFile/{predictionName}";
            else url = baseUrl.BuildFunctionUrl("GetPredictionSet", $"name={predictionName}", apiKey);
            var response = await httpService.Get<List<RuleModel>>(url);
            if (!response.Success)
            {
                throw new ApplicationException(await response.GetBody());
            }
            return response.Response;
        }

        public async Task<List<PredictionSet>> GetPredictions()
        {
            if (string.IsNullOrEmpty(baseUrl)) url = $"api/rules/RuleFile";
            else url = baseUrl.BuildFunctionUrl("GetPredictionSets", $"", apiKey);
            var response = await httpService.Get<List<PredictionSet>>(url);
            if (!response.Success)
            {
                throw new ApplicationException(await response.GetBody());
            }
            return response.Response;
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

        public async Task<RuleInfo> GetRuleInfo(string source)
        {
            if (string.IsNullOrEmpty(baseUrl)) url = $"api/rules/RuleInfo/{source}";
            else url = baseUrl.BuildFunctionUrl("GetRuleInfo", $"", apiKey);
            var response = await httpService.Get<RuleInfo>(url);
            if (!response.Success)
            {
                throw new ApplicationException(await response.GetBody());
            }
            return response.Response;
        }

        public async Task<List<RuleModel>> GetRules(string source)
        {
            if (string.IsNullOrEmpty(baseUrl)) url = $"api/rules/{source}";
            else url = baseUrl.BuildFunctionUrl("GetDataRules", $"name={source}", apiKey);
            var response = await httpService.Get<List<RuleModel>>(url);
            if (!response.Success)
            {
                throw new ApplicationException(await response.GetBody());
            }
            return response.Response;
        }

        public async Task InsertPrediction(PredictionSet predictionSet, string predictionName)
        {
            if (string.IsNullOrEmpty(baseUrl)) url = $"api/rules/RuleFile/{predictionName}";
            else url = baseUrl.BuildFunctionUrl("SavePredictionSet", $"name={predictionName}", apiKey);
            var response = await httpService.Post(url, predictionSet);
            if (!response.Success)
            {
                throw new ApplicationException(await response.GetBody());
            }
        }

        public async Task InsertRule(RuleModel rule, string source)
        {
            if (string.IsNullOrEmpty(baseUrl)) url = $"api/rules/{source}";
            else url = baseUrl.BuildFunctionUrl("SaveDataRule", $"name={source}", apiKey);
            var response = await httpService.Post(url, rule);
            if (!response.Success)
            {
                throw new ApplicationException(await response.GetBody());
            }
        }

        public async Task UpdateRule(RuleModel rule, string source, int id)
        {
            if (string.IsNullOrEmpty(baseUrl)) url = $"api/rules/{source}/{id}";
            else url = baseUrl.BuildFunctionUrl("UpdateDataRule", $"name={source}&id={id}", apiKey);
            var response = await httpService.Put(url, rule);
            if (!response.Success)
            {
                throw new ApplicationException(await response.GetBody());
            }
        }

        public async Task<List<RuleFunctions>> GetFunctions(string source)
        {
            if (string.IsNullOrEmpty(baseUrl)) url = $"api/functions/{source}";
            else url = baseUrl.BuildFunctionUrl("GetRuleFunctions", $"name={source}", apiKey);
            var response = await httpService.Get<List<RuleFunctions>>(url);
            if (!response.Success)
            {
                throw new ApplicationException(await response.GetBody());
            }
            return response.Response;
        }

        public async Task InsertFunction(RuleFunctions function, string source)
        {
            if (string.IsNullOrEmpty(baseUrl)) url = $"api/functions/{source}";
            else url = baseUrl.BuildFunctionUrl("SaveRuleFunction", $"name={source}", apiKey);
            var response = await httpService.Post(url, function);
            if (!response.Success)
            {
                throw new ApplicationException(await response.GetBody());
            }
        }

        public async Task UpdateFunction(RuleFunctions function, string source, int id)
        {
            if (string.IsNullOrEmpty(baseUrl)) url = $"api/functions/{source}/{id}";
            else url = baseUrl.BuildFunctionUrl("UpdateRuleFunction", $"name={source}&id={id}", apiKey);
            var response = await httpService.Put(url, function);
            if (!response.Success)
            {
                throw new ApplicationException(await response.GetBody());
            }
        }

        public async Task<RuleFunctions> GetFunction(string source, int id)
        {
            if (string.IsNullOrEmpty(baseUrl)) url = $"api/functions/{source}/{id}";
            else url = baseUrl.BuildFunctionUrl("GetRuleFunction", $"name={source}&id={id}", apiKey);
            var response = await httpService.Get<RuleFunctions>(url);
            if (!response.Success)
            {
                throw new ApplicationException(await response.GetBody());
            }
            return response.Response;
        }

        public async Task DeleteFunction(string source, int id)
        {
            if (string.IsNullOrEmpty(baseUrl)) url = $"api/functions/{source}/{id}";
            else url = baseUrl.BuildFunctionUrl("DeleteRuleFunction", $"name={source}&id={id}", apiKey);
            var response = await httpService.Delete(url);
            if (!response.Success)
            {
                throw new ApplicationException(await response.GetBody());
            }
        }
    }
}
