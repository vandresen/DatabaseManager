using DatabaseManager.Services.Reports.Extensions;
using DatabaseManager.Services.Reports.Models;

namespace DatabaseManager.Services.Reports.Services
{
    public class IndexAccess : BaseService, IIndexAccess
    {
        private readonly IHttpClientFactory _clientFactory;

        public IndexAccess(IHttpClientFactory clientFactory) : base(clientFactory)
        {
            _clientFactory = clientFactory;
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
    }
}
