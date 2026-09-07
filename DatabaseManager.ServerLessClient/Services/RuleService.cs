using DatabaseManager.ServerLessClient.Models;
using DatabaseManager.ServerLessClient.Helpers;
using Newtonsoft.Json;

namespace DatabaseManager.ServerLessClient.Services
{
    public class RuleService : BaseService, IRuleService
    {
        private readonly IHttpClientFactory _clientFactory;
        private readonly BlazorSingletonService _settings;

        public RuleService(IHttpClientFactory clientFactory, BlazorSingletonService settings) : base(clientFactory)
        {
            _clientFactory = clientFactory;
            _settings = settings;
        }

        public async Task DeleteFunctionAsync(int id)
        {
            string url = _settings.DataRuleAPI.BuildFunctionUrl($"/Function", $"Id={id}", "");
            Console.WriteLine($"DeleteFunctionAsync: url = {url}");
            ResponseDto response = await this.SendAsync<ResponseDto>(new ApiRequest()
            {
                ApiType = SD.ApiType.DELETE,
                Url = url
            });
            if (!response.IsSuccess)
            {

                Console.WriteLine(String.Join("There is a problem deleting the function; ", response.ErrorMessages));
                throw new ApplicationException(String.Join("There is a problem deleting the function; ", response.ErrorMessages));
            }
        }

        public async Task DeletePredictionAsync(int id)
        {
            string url = _settings.DataRuleAPI.BuildFunctionUrl($"/PredictionSet", $"id={id}", "");
            Console.WriteLine($"GetFunctionsAsync: url = {url}");
            ResponseDto response = await this.SendAsync<ResponseDto>(new ApiRequest()
            {
                ApiType = SD.ApiType.DELETE,
                Url = url
            });
            if (!response.IsSuccess)
            {

                Console.WriteLine(String.Join("There is a problem deleting the prediction set; ", response.ErrorMessages));
                throw new ApplicationException(String.Join("There is a problem deleting the prediction set; ", response.ErrorMessages));
            }
        }

        public async Task DeleteRuleAsync(int id)
        {
            string url = _settings.DataRuleAPI.BuildFunctionUrl($"/Rules", $"Id={id}", "");
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

        public Task<RuleFunction> GetFunctionAsync(int id)
        {
            throw new NotImplementedException();
        }

        public async Task<List<RuleFunctionDto>> GetFunctionsAsync()
        {
            List<RuleFunctionDto> result = new List<RuleFunctionDto>();
            string url = _settings.DataRuleAPI.BuildFunctionUrl($"/Function", $"", "");
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

        public async Task<PredictionSet> GetPredictionAsync(string predictionName)
        {
            PredictionSet result = new PredictionSet();
            string url = _settings.DataRuleAPI.BuildFunctionUrl($"/PredictionSet", $"Name={predictionName}", "");
            Console.WriteLine($"GetPredictionAsync: url = {url}");
            ResponseDto response = await this.SendAsync<ResponseDto>(new ApiRequest()
            {
                ApiType = SD.ApiType.GET,
                Url = url
            });
            if (response.IsSuccess)
            {
                string json = response.Result.ToString();
                Console.WriteLine($"GetPredictionsAsync: json = {json}");
                var predictionSets = JsonConvert.DeserializeObject<List<PredictionSet>>(json);
                if (predictionSets != null && predictionSets.Count > 0)
                {
                    result = predictionSets[0];
                }
                else
                {
                    result = null;
                }
            }
            else
            {
                Console.WriteLine(String.Join("There is a problem getting prediction sets; ", response.ErrorMessages));
                throw new ApplicationException(String.Join("There is a problem getting prediction sets; ", response.ErrorMessages));
            }
            return result;
        }

        public async Task<List<PredictionSet>> GetPredictionsAsync()
        {
            List<PredictionSet> result = new List<PredictionSet>();
            string url = _settings.DataRuleAPI.BuildFunctionUrl($"/PredictionSet", $"", "");
            Console.WriteLine($"GetPredictionsAsync: url = {url}");
            ResponseDto response = await this.SendAsync<ResponseDto>(new ApiRequest()
            {
                ApiType = SD.ApiType.GET,
                Url = url
            });
            if (response.IsSuccess)
            {
                string json = response.Result.ToString();
                Console.WriteLine($"GetPredictionsAsync: json = {json}");
                result = JsonConvert.DeserializeObject<List<PredictionSet>>(json);
            }
            else
            {
                Console.WriteLine(String.Join("There is a problem getting prediction sets; ", response.ErrorMessages));
                throw new ApplicationException(String.Join("There is a problem getting prediction sets; ", response.ErrorMessages));
            }
            return result;
        }

        public async Task<RuleModelDto> GetRuleAsync(string source, int id)
        {
            RuleModelDto result = new RuleModelDto();
            string url = _settings.DataRuleAPI.BuildFunctionUrl($"/Rule", $"Name={source}&Id={id}", "");
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
            string url = _settings.DataRuleAPI.BuildFunctionUrl($"/Rules", $"", "");
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

        public async Task InsertFunctionAsync(RuleFunction function)
        {
            string url = _settings.DataRuleAPI.BuildFunctionUrl($"/Function", $"", "");
            Console.WriteLine($"InsertRulesAsync: url = {url}");
            ResponseDto response = await this.SendAsync<ResponseDto>(new ApiRequest()
            {
                ApiType = SD.ApiType.POST,
                Data = function,
                Url = url
            });
            if (!response.IsSuccess)
            {

                Console.WriteLine(String.Join("There is a problem inserting function; ", response.ErrorMessages));
                throw new ApplicationException(String.Join("There is a problem inserting function; ", response.ErrorMessages));
            }
        }

        public async Task InsertPredictionAsync(PredictionSet predictionSet)
        {
            string url = _settings.DataRuleAPI.BuildFunctionUrl($"/PredictionSet", $"", "");
            Console.WriteLine($"InsertPredictionAsync: url = {url}");
            ResponseDto response = await this.SendAsync<ResponseDto>(new ApiRequest()
            {
                ApiType = SD.ApiType.POST,
                Data = predictionSet,
                Url = url
            });
            if (!response.IsSuccess)
            {

                Console.WriteLine(String.Join("There is a problem inserting prediction set; ", response.ErrorMessages));
                throw new ApplicationException(String.Join("There is a problem inserting prediction set; ", response.ErrorMessages));
            }
        }

        public async Task InsertRuleAsync(RuleModel rule)
        {
            string url = _settings.DataRuleAPI.BuildFunctionUrl($"/Rules", $"", "");
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

        public async Task UpdateFunctionAsync(RuleFunction function)
        {
            string url = _settings.DataRuleAPI.BuildFunctionUrl($"/Function", $"", "");
            Console.WriteLine($"UpdateFunctionAsync: url = {url}");
            ResponseDto response = await this.SendAsync<ResponseDto>(new ApiRequest()
            {
                ApiType = SD.ApiType.PUT,
                Data = function,
                Url = url
            });
            if (!response.IsSuccess)
            {

                Console.WriteLine(String.Join("There is a problem updating function; ", response.ErrorMessages));
                throw new ApplicationException(String.Join("There is a problem updating function; ", response.ErrorMessages));
            }
        }

        public async Task UpdateRuleAsync(RuleModel rule)
        {
            string url = _settings.DataRuleAPI.BuildFunctionUrl($"/Rules", $"", "");
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
