using DatabaseManager.Shared;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace DatabaseManager.Common.Services
{
    public class DataSourcesClient: IDataSources
    {
        private readonly IHttpService httpService;
        private string url = "api/source";

        public DataSourcesClient(IHttpService httpService)
        {
            this.httpService = httpService;
        }

        public async Task<List<ConnectParameters>> GetSources()
        {
            var response = await httpService.Get<List<ConnectParameters>>(url);
            if (!response.Success)
            {
                throw new ApplicationException(await response.GetBody());
            }
            return response.Response;
        }

        public async Task<ConnectParameters> GetSource(string Name)
        {
            var response = await httpService.Get<ConnectParameters>($"{url}/{Name}");
            if (!response.Success)
            {
                throw new ApplicationException(await response.GetBody());
            }
            return response.Response;
        }

        public async Task CreateSource(ConnectParameters connectParameters)
        {
            var response = await httpService.Post(url, connectParameters);
            if (!response.Success)
            {
                throw new ApplicationException(await response.GetBody());
            }
        }

        public async Task UpdateSource(ConnectParameters connectParameters)
        {
            var response = await httpService.Put(url, connectParameters);
            if (!response.Success)
            {
                throw new ApplicationException(await response.GetBody());
            }
        }

        public async Task DeleteSource(string Name)
        {
            var response = await httpService.Delete($"{url}/{Name}");
            if (!response.Success)
            {
                throw new ApplicationException(await response.GetBody());
            }
        }
    }
}
