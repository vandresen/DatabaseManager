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

        public async Task InsertFunction(RuleFunctions function, string source)
        {
            var response = await httpService.Post($"{url}/{source}", function);
            if (!response.Success)
            {
                throw new ApplicationException(await response.GetBody());
            }
        }
    }
}
