using DatabaseManager.ServerLessClient.Models;
using DatabaseManager.ServerLessClient.Helpers;

namespace DatabaseManager.ServerLessClient.Services
{
    public class DataConfigurationService : BaseService, IDataConfigurationService
    {
        private readonly IHttpClientFactory _clientFactory;
        private readonly BlazorSingletonService _settings;
        private readonly string folder = "connectdefinition";
        private string url;

        public DataConfigurationService(IHttpClientFactory clientFactory, BlazorSingletonService settings) : base(clientFactory)
        {
            _clientFactory = clientFactory;
            _settings = settings;
        }

        public Task<T> DeleteRecord<T>(string name)
        {
            throw new NotImplementedException();
        }

        public async Task<T> GetRecord<T>(string name)
        {
            ResponseDto responseDto = new ResponseDto();
            url = SD.DataConfigurationAPIBase.BuildFunctionUrl("/api/GetDataConfiguration", $"folder={folder}&name={name}", SD.DataConfigurationKey);
            Console.WriteLine($"GetRecord URL:{url}");
            return await this.SendAsync<T>(new ApiRequest()
            {
                ApiType = SD.ApiType.GET,
                AzureStorage = _settings.AzureStorage,
                Url = url
            });
        }

        public Task<T> GetRecords<T>()
        {
            throw new NotImplementedException();
        }

        public Task<T> SaveRecords<T>(string name, object body)
        {
            throw new NotImplementedException();
        }
    }
}
