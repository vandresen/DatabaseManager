using DatabaseManager.Shared;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace DatabaseManager.Client.Helpers
{
    public class CreateIndex : ICreateIndex
    {
        private readonly IHttpService httpService;
        private string url = "api/createindex";

        public CreateIndex(IHttpService httpService)
        {
            this.httpService = httpService;
        }

        public async Task<List<string>> GetTaxonomies()
        {
            var response = await httpService.Get<List<string>>(url);
            if (!response.Success)
            {
                throw new ApplicationException(await response.GetBody());
            }
            return response.Response;
        }

        public async Task<CreateIndexParameters> GetTaxonomy(string Name)
        {
            var response = await httpService.Get<CreateIndexParameters>($"{url}/{Name}");
            if (!response.Success)
            {
                throw new ApplicationException(await response.GetBody());
            }
            return response.Response;
        }

        public async Task CreateChildIndexes(CreateIndexParameters iParams)
        {
            var response = await httpService.Post($"{url}/children", iParams);
            if (!response.Success)
            {
                throw new ApplicationException(await response.GetBody());
            }
        }

        public async Task<List<ParentIndexNodes>> CreateParentNodes(CreateIndexParameters iParameters)
        {
            var response = await httpService.Post<CreateIndexParameters, List<ParentIndexNodes>>(url, iParameters);
            if (!response.Success)
            {
                throw new ApplicationException(await response.GetBody());
            }

            return response.Response;
        }
    }
}
