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

        public async Task<RuleModel> GetRule(string source, int id)
        {
            var response = await httpService.Get<RuleModel>($"{url}/{source}/{id}");
            if (!response.Success)
            {
                throw new ApplicationException(await response.GetBody());
            }
            return response.Response;
        }

        public async Task UpdateRule(RuleModel rule, string source, int id)
        {
            var response = await httpService.Put($"{url}/{source}/{id}", rule);
            if (!response.Success)
            {
                throw new ApplicationException(await response.GetBody());
            }
        }

        public async Task DeleteRule(string source, int id)
        {
            var response = await httpService.Delete($"{url}/{source}/{id}");
            if (!response.Success)
            {
                throw new ApplicationException(await response.GetBody());
            }
        }
    }
}
