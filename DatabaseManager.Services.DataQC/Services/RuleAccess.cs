using DatabaseManager.Services.DataQC.Extensions;
using DatabaseManager.Services.DataQC.Models;

namespace DatabaseManager.Services.DataQC.Services
{
    public class RuleAccess : BaseService, IRuleAccess
    {
        private readonly IHttpClientFactory _clientFactory;

        public RuleAccess(IHttpClientFactory clientFactory) : base(clientFactory)
        {
            _clientFactory = clientFactory;
        }
        public async Task<T> GetRule<T>(int id, string sourceName)
        {
            string url = SD.RuleAPIBase.BuildFunctionUrl($"/Rule", $"Name={sourceName}&Id={id}", SD.RuleKey);
            return await this.SendAsync<T>(new ApiRequest()
            {
                ApiType = SD.ApiType.GET,
                Url = url
            });
        }
    }
}
