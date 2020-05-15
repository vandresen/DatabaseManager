using DatabaseManager.Shared;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace DatabaseManager.Client.Helpers
{
    public class Rules: IRules
    {
        private readonly IHttpService httpService;
        private string url = "api/rules";

        public Rules(IHttpService httpService)
        {
            this.httpService = httpService;
        }

        public async Task<List<RuleModel>> GetRules(string source)
        {
            var response = await httpService.Get<List<RuleModel>>($"{url}/{source}");
            if (!response.Success)
            {
                throw new ApplicationException(await response.GetBody());
            }
            return response.Response;
        }
    }
}
