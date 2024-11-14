using DatabaseManager.Services.Reports.Extensions;
using DatabaseManager.Services.Reports.Models;
using Microsoft.Extensions.Logging;

namespace DatabaseManager.Services.Reports.Services
{
    public class IndexAccess : BaseService, IIndexAccess
    {
        private readonly IHttpClientFactory _clientFactory;
        private readonly ILogger<IndexAccess> _logger;

        public IndexAccess(IHttpClientFactory clientFactory, ILogger<IndexAccess> logger) : base(clientFactory)
        {
            _clientFactory = clientFactory;
            _logger = logger;
        }

        public async Task<T> GetIndexFailures<T>(string dataSource, string project, string dataType, string qcString)
        {
            string url = SD.IndexAPIBase.BuildFunctionUrl($"/QueryIndex", 
                $"Name={dataSource}&DataType={dataType}&Project={project}&QcString={qcString}", SD.IndexKey);
            return await this.SendAsync<T>(new ApiRequest()
            {
                ApiType = SD.ApiType.GET,
                Url = url
            });
        }

        public async Task<T> GetRootIndex<T>(string dataSource, string project)
        {
            string url = "";
            if (SD.Sqlite)
            {
                url = SD.IndexAPIBase.BuildFunctionUrl($"/Index/1", $"project={project}", SD.IndexKey);
            }
            else
            {
                url = SD.IndexAPIBase.BuildFunctionUrl("/DmIndexes", $"Name={dataSource}&Node=/&Level=0", SD.IndexKey);
            }
            _logger.LogInformation($"Url = {url}");
            return await this.SendAsync<T>(new ApiRequest()
            {
                ApiType = SD.ApiType.GET,
                Url = url
            });
        }
    }
}
