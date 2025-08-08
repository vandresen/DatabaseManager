using DatabaseManager.ServerLessClient.Models;
using DatabaseManager.ServerLessClient.Helpers;

namespace DatabaseManager.ServerLessClient.Services
{
    public class DataConfigurationService : BaseService, IDataConfigurationService
    {
        private readonly IHttpClientFactory _clientFactory;
        private readonly BlazorSingletonService _settings;
        private readonly string _dataConfigurationAPIBase;
        private readonly string _dataConfigurationKey;
        private readonly string folder = "connectdefinition";
        private string url;

        public DataConfigurationService(IHttpClientFactory clientFactory, BlazorSingletonService settings,
            IConfiguration configuration) : base(clientFactory)
        {
            _clientFactory = clientFactory;
            _settings = settings;
            _dataConfigurationAPIBase = configuration["ServiceUrls:DataConfigurationAPI"]
                ?? throw new InvalidOperationException("Missing ServiceUrls:DataConfigurationAPI");
            _dataConfigurationKey = configuration["ServiceUrls:DataConfigurationKey"]
                ?? throw new InvalidOperationException("Missing ServiceUrls:DataConfigurationKey");
        }

        public Task<T> DeleteRecord<T>(string name)
        {
            throw new NotImplementedException();
        }

        public async Task<T> GetRecord<T>(string name)
        {
            ResponseDto responseDto = new ResponseDto();
            url = _dataConfigurationAPIBase.BuildFunctionUrl("/api/GetDataConfiguration", $"folder={folder}&name={name}", _dataConfigurationKey);
            Console.WriteLine($"GetRecord URL:{url}");
            return await this.SendAsync<T>(new ApiRequest()
            {
                ApiType = SD.ApiType.GET,
                AzureStorage = _settings.AzureStorage,
                Url = url
            });
        }

        public async Task<T> GetRecords<T>()
        {
            url = _dataConfigurationAPIBase.BuildFunctionUrl("/api/GetDataConfiguration", $"folder={folder}", _dataConfigurationKey);
            Console.WriteLine($"GetRecords URL:{url}");
            return await this.SendAsync<T>(new ApiRequest()
            {
                ApiType = SD.ApiType.GET,
                AzureStorage = _settings.AzureStorage,
                Url = url
            });
        }

        public async Task<T> SaveRecords<T>(string name, object body)
        {
            url = _dataConfigurationAPIBase.BuildFunctionUrl("/api/DataConfiguration", $"folder={folder}&name={name}", _dataConfigurationKey);
            Console.WriteLine(url);
            return await this.SendAsync<T>(new ApiRequest()
            {
                ApiType = SD.ApiType.POST,
                AzureStorage = _settings.AzureStorage,
                Url = url,
                Data = body
            });
        }
    }
}
