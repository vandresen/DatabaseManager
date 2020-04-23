using DatabaseManager.Shared;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace DatabaseManager.Client.Helpers
{
    public class IndexData : IIndexData
    {
        private readonly IHttpService httpService;
        private string url = "api/index";

        public IndexData(IHttpService httpService)
        {
            this.httpService = httpService;
        }

        public async Task<List<DmsIndex>> GetIndex(string source)
        {
            var response = await httpService.Get<List<DmsIndex>>($"{url}/{source}");
            if (!response.Success)
            {
                throw new ApplicationException(await response.GetBody());
            }
            return response.Response;
        }

        public async Task<List<DmsIndex>> GetChildren(string source, int id)
        {
            var response = await httpService.Get<List<DmsIndex>>($"{url}/{source}/{id}");
            if (!response.Success)
            {
                throw new ApplicationException(await response.GetBody());
            }
            return response.Response;
        }
    }
}
