using DatabaseManager.BlazorComponents.Extensions;
using DatabaseManager.Shared;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DatabaseManager.BlazorComponents.Services
{
    public class DataSourcesServerLess : IDataSources
    {
        private readonly IHttpService httpService;
        private readonly string baseUrl;
        private readonly string apiKey;

        public DataSourcesServerLess(IHttpService httpService, SingletonServices settings)
        {
            this.httpService = httpService;
            baseUrl = settings.BaseUrl;
            apiKey = settings.ApiKey;
        }
        public async Task CreateSource(ConnectParameters connectParameters)
        {
            string url = baseUrl.BuildFunctionUrl("SaveDataSource", $"", apiKey);
            var response = await httpService.Post(url, connectParameters);
            if (!response.Success)
            {
                throw new ApplicationException(await response.GetBody());
            }
        }

        public async Task DeleteSource(string Name)
        {
            string url = baseUrl.BuildFunctionUrl("DeleteDataSource", $"name={Name}", apiKey);
            var response = await httpService.Delete(url);
            if (!response.Success)
            {
                throw new ApplicationException(await response.GetBody());
            }
        }

        public async Task<ConnectParameters> GetSource(string Name)
        {
            string url = baseUrl.BuildFunctionUrl("GetDataSource", $"name={Name}", apiKey);
            var response = await httpService.Get<ConnectParameters>(url);
            if (!response.Success)
            {
                throw new ApplicationException(await response.GetBody());
            }
            return response.Response;
        }

        public async Task<List<ConnectParameters>> GetSources()
        {
            string url = baseUrl.BuildFunctionUrl("GetDataSources", $"", apiKey);
            Console.WriteLine($"GetSources: url = {url}");
            var response = await httpService.Get<List<ConnectParameters>>(url);
            if (!response.Success)
            {
                throw new ApplicationException(await response.GetBody());
            }
            return response.Response;
        }

        public async Task UpdateSource(ConnectParameters connectParameters)
        {
            string url = baseUrl.BuildFunctionUrl("UpdateDataSource", $"", apiKey);
            var response = await httpService.Put(url, connectParameters);
            if (!response.Success)
            {
                throw new ApplicationException(await response.GetBody());
            }
        }
    }
}
