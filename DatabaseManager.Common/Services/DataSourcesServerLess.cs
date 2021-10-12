using DatabaseManager.Shared;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace DatabaseManager.Common.Services
{
    public class DataSourcesServerLess : IDataSources
    {
        private readonly IHttpService httpService;
        private readonly string baseUrl;

        public DataSourcesServerLess(IHttpService httpService, SingletonService settings)
        {
            this.httpService = httpService;
            baseUrl = settings.BaseUrl;
        }
        public async Task CreateSource(ConnectParameters connectParameters)
        {
            string url = baseUrl + "SaveDataSource";
            var response = await httpService.Post(url, connectParameters);
            if (!response.Success)
            {
                throw new ApplicationException(await response.GetBody());
            }
        }

        public async Task DeleteSource(string Name)
        {
            string url = baseUrl + "DeleteDataSource?name=" + Name;
            var response = await httpService.Delete(url);
            if (!response.Success)
            {
                throw new ApplicationException(await response.GetBody());
            }
        }

        public async Task<ConnectParameters> GetSource(string Name)
        {
            string url = baseUrl + "GetDataSource?name=" + Name;
            var response = await httpService.Get<ConnectParameters>(url);
            if (!response.Success)
            {
                throw new ApplicationException(await response.GetBody());
            }
            return response.Response;
        }

        public async Task<List<ConnectParameters>> GetSources()
        {
            string url = baseUrl + "GetDataSources";
            var response = await httpService.Get<List<ConnectParameters>>(url);
            if (!response.Success)
            {
                throw new ApplicationException(await response.GetBody());
            }
            return response.Response;
        }

        public async Task UpdateSource(ConnectParameters connectParameters)
        {
            string url = baseUrl + "UpdateDataSource";
            Console.WriteLine(url);
            var response = await httpService.Put(url, connectParameters);
            if (!response.Success)
            {
                throw new ApplicationException(await response.GetBody());
            }
        }
    }
}
