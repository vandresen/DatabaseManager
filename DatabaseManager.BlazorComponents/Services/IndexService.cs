using DatabaseManager.BlazorComponents.Extensions;
using DatabaseManager.BlazorComponents.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

namespace DatabaseManager.BlazorComponents.Services
{
    public class IndexService : BaseService, IIndexService
    {
        private readonly IHttpClientFactory _clientFactory;

        public IndexService(IHttpClientFactory clientFactory) : base(clientFactory)
        {
            _clientFactory = clientFactory;
        }
        public async Task<T> GetDmIndexesAsync<T>(string source)
        {
            string baseUrl = SD.IndexAPIBase + "/api/DmIndexes";
            string url = SD.IndexAPIBase.BuildFunctionUrl("/api/DmIndexes", $"name={source}", SD.IndexKey);
            Console.WriteLine($"GetDmIndexesAsync: url = {url}");
            return await this.SendAsync<T>(new ApiRequest()
            {
                ApiType = SD.ApiType.GET,
                Url = url
            });
        }
    }
}
