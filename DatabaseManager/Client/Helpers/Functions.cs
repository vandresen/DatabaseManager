using DatabaseManager.Common.Services;
using DatabaseManager.Shared;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace DatabaseManager.Client.Helpers
{
    public class Functions: IFunctions
    {
        private readonly IHttpService httpService;
        private string url = "api/functions";

        public Functions(IHttpService httpService)
        {
            this.httpService = httpService;
        }

        public async Task<List<RuleFunctions>> GetFunctions(string source)
        {
            var response = await httpService.Get<List<RuleFunctions>>($"{url}/{source}");
            if (!response.Success)
            {
                throw new ApplicationException(await response.GetBody());
            }
            return response.Response;
        }

        public async Task<RuleFunctions> GetFunction(string source, int id)
        {
            var response = await httpService.Get<RuleFunctions>($"{url}/{source}/{id}");
            if (!response.Success)
            {
                throw new ApplicationException(await response.GetBody());
            }
            return response.Response;
        }

        public async Task InsertFunction(RuleFunctions function, string source)
        {
            var response = await httpService.Post($"{url}/{source}", function);
            if (!response.Success)
            {
                throw new ApplicationException(await response.GetBody());
            }
        }

        public async Task UpdateFunction(RuleFunctions function, string source, int id)
        {
            var response = await httpService.Put($"{url}/{source}/{id}", function);
            if (!response.Success)
            {
                throw new ApplicationException(await response.GetBody());
            }
        }

        public async Task DeleteFunction(string source, int id)
        {
            var response = await httpService.Delete($"{url}/{source}/{id}");
            if (!response.Success)
            {
                throw new ApplicationException(await response.GetBody());
            }
        }
    }
}
