﻿using DatabaseManager.ServerLessClient.Models;
using DatabaseManager.ServerLessClient.Helpers;
using Newtonsoft.Json;
using DatabaseManager.BlazorComponents.Services;

namespace DatabaseManager.ServerLessClient.Services
{
    public class RuleService : BaseService, IRuleService
    {
        private readonly IHttpClientFactory _clientFactory;
        private readonly BlazorSingletonService _settings;

        public RuleService(IHttpClientFactory clientFactory, BlazorSingletonService settings) : base(clientFactory)
        {
            _clientFactory = clientFactory;
        }

        public async Task DeleteRuleAsync(int id)
        {
            string url = SD.DataRuleAPIBase.BuildFunctionUrl($"/Rules", $"Id={id}", SD.DataRuleKey);
            Console.WriteLine($"DeleteRuleAsync: url = {url}");
            ResponseDto response = await this.SendAsync<ResponseDto>(new ApiRequest()
            {
                ApiType = SD.ApiType.DELETE,
                Url = url
            });
            if (!response.IsSuccess)
            {

                Console.WriteLine(String.Join("There is a problem deleting rule; ", response.ErrorMessages));
                throw new ApplicationException(String.Join("There is a problem deleting rule; ", response.ErrorMessages));
            }
        }

        public async Task<List<RuleFunctionDto>> GetFunctionsAsync()
        {
            List<RuleFunctionDto> result = new List<RuleFunctionDto>();
            string url = SD.DataRuleAPIBase.BuildFunctionUrl($"/Function", $"", SD.DataRuleKey);
            Console.WriteLine($"GetFunctionsAsync: url = {url}");
            ResponseDto response = await this.SendAsync<ResponseDto>(new ApiRequest()
            {
                ApiType = SD.ApiType.GET,
                Url = url
            });
            if (response.IsSuccess)
            {
                result = JsonConvert.DeserializeObject<List<RuleFunctionDto>>(response.Result.ToString());
            }
            else
            {
                Console.WriteLine($"No success");
            }
            return result;
        }

        public async Task<RuleModelDto> GetRuleAsync(string source, int id)
        {
            RuleModelDto result = new RuleModelDto();
            string url = SD.DataRuleAPIBase.BuildFunctionUrl($"/Rule", $"Name={source}&Id={id}", SD.DataRuleKey);
            Console.WriteLine($"GetRuleAsync: url = {url}");
            ResponseDto response =  await this.SendAsync<ResponseDto>(new ApiRequest()
            {
                ApiType = SD.ApiType.GET,
                Url = url
            });
            Console.WriteLine($"{response.Result.ToString()}");
            if (response.IsSuccess)
            {
                result = JsonConvert.DeserializeObject<RuleModelDto>(response.Result.ToString());
            }
            else
            {
                Console.WriteLine($"No success");
            }
            return result;
        }

        public async Task<List<RuleModelDto>> GetRulesAsync()
        {
            List<RuleModelDto> result = new List<RuleModelDto>();
            string url = SD.DataRuleAPIBase.BuildFunctionUrl($"/Rules", $"", "");
            Console.WriteLine($"GetRuleAsync: url = {url}");
            ResponseDto response = await this.SendAsync<ResponseDto>(new ApiRequest()
            {
                ApiType = SD.ApiType.GET,
                Url = url
            });
            Console.WriteLine($"{response.Result.ToString()}");
            if (response.IsSuccess)
            {
                result = JsonConvert.DeserializeObject<List<RuleModelDto>>(response.Result.ToString());
            }
            else
            {
                Console.WriteLine($"No success");
            }
            return result;
        }

        public async Task InsertRuleAsync(RuleModel rule)
        {
            string url = SD.DataRuleAPIBase.BuildFunctionUrl($"/Rules", $"", SD.DataRuleKey);
            Console.WriteLine($"InsertRulesAsync: url = {url}");
            ResponseDto response = await this.SendAsync<ResponseDto>(new ApiRequest()
            {
                ApiType = SD.ApiType.POST,
                Data = rule,
                Url = url
            });
            if (!response.IsSuccess)
            {
                
                Console.WriteLine(String.Join("There is a problem inserting rule; ", response.ErrorMessages));
                throw new ApplicationException(String.Join("There is a problem inserting rule; ", response.ErrorMessages));
            }
        }

        public async Task UpdateRuleAsync(RuleModel rule)
        {
            string url = SD.DataRuleAPIBase.BuildFunctionUrl($"/Rules", $"", SD.DataRuleKey);
            Console.WriteLine($"UpdateRulesAsync: url = {url}");
            ResponseDto response = await this.SendAsync<ResponseDto>(new ApiRequest()
            {
                ApiType = SD.ApiType.POST,
                Data = rule,
                Url = url
            });
            if (!response.IsSuccess)
            {
                Console.WriteLine(String.Join("There is a problem updating rule; ", response.ErrorMessages));
                throw new ApplicationException(String.Join("There is a problem updating rule; ", response.ErrorMessages));
            }
        }
    }
}
