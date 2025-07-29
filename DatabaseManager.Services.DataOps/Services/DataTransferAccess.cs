using DatabaseManager.Services.DataOps.Extensions;
using DatabaseManager.Services.DataOps.Models;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System.Security.Policy;

namespace DatabaseManager.Services.DataOps.Services
{
    

    public class DataTransferAccess : BaseService, IDataTransferAccess
    {
        private readonly IConfiguration _configuration;
        private readonly IHttpClientFactory _clientFactory;
        private readonly string _ruleAPIBase;
        private readonly string _ruleKey;

        public DataTransferAccess(IConfiguration configuration,
            IHttpClientFactory clientFactory, ILoggerFactory loggerFactory) : base(clientFactory)
        {
            _configuration = configuration;
            _clientFactory = clientFactory;
            _ruleAPIBase = configuration.GetValue<string>("DataTransferAPI") ?? throw new ArgumentNullException("DataTransferAPI");
            _ruleKey = configuration.GetValue<string>("DataTransferKey") ?? throw new ArgumentNullException("DataTransferKey");
        }

        public async Task<T> Copy<T>(TransferParameters transferParameters, string azureStorage)
        {
            string url = "";
            if (transferParameters.SourceType == "DataBase")
            {
                url = _ruleAPIBase.BuildFunctionUrl($"/CopyDatabaseObject", $"", _ruleKey);
            }
            else if (transferParameters.SourceType == "File")
            {
                if (transferParameters.SourceDataType == "Logs")
                {
                    url = _ruleAPIBase.BuildFunctionUrl($"/CopyLASObject", $"", _ruleKey);
                }
                else
                {
                    url = _ruleAPIBase.BuildFunctionUrl($"/CopyCSVObject", $"", _ruleKey);
                }
            }
            else
            {
                throw new ArgumentException("Copy: Bad source type ");
            }
            return await this.SendAsync<T>(new ApiRequest()
            {
                ApiType = SD.ApiType.POST,
                AzureStorage = azureStorage,
                Url = url,
                Data = transferParameters
            });
        }

        public async Task<T> DeleteTable<T>(string dataSourceName, string table, string azureStorage)
        {
            string url = _ruleAPIBase.BuildFunctionUrl($"/DeleteObject", $"Name={dataSourceName}&Table={table}", _ruleKey);
            return await this.SendAsync<T>(new ApiRequest()
            {
                ApiType = SD.ApiType.DELETE,
                AzureStorage = azureStorage,
                Url = url
            });
        }

        public async Task<T> GetDataObjects<T>(string sourceName, string azureStorage)
        {
            string url = _ruleAPIBase.BuildFunctionUrl($"/GetDataObjects", $"Name={sourceName}", _ruleKey);
            return await this.SendAsync<T>(new ApiRequest()
            {
                ApiType = SD.ApiType.GET,
                AzureStorage = azureStorage,
                Url = url
            });
        }
    }
}
