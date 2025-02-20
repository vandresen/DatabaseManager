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
    }
}
