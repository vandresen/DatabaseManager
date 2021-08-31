using DatabaseManager.Common.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace DatabaseManager.Client.Helpers
{
    public class DataOps: IDataOps
    {
        private readonly IHttpService httpService;
        private string url = "api/dataops";

        public DataOps(IHttpService httpService)
        {
            this.httpService = httpService;
        }

        public async Task<List<string>> GetPipelines()
        {
            var response = await httpService.Get<List<string>>($"{url}");
            if (!response.Success)
            {
                throw new ApplicationException(await response.GetBody());
            }
            return response.Response;
        }

        public async Task ProcessPipeline(string name)
        {
            var response = await httpService.Post($"{url}/{name}");
            if (!response.Success)
            {
                throw new ApplicationException(await response.GetBody());
            }
        }

        public async Task CreatePipeline(string name)
        {
            var response = await httpService.Post($"{url}/CreatePipeline/{name}");
            if (!response.Success)
            {
                throw new ApplicationException(await response.GetBody());
            }
        }

        public async Task DeletePipeline(string Name)
        {
            var response = await httpService.Delete($"{url}/{Name}");
            if (!response.Success)
            {
                throw new ApplicationException(await response.GetBody());
            }
        }
    }
}
