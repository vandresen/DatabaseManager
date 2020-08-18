using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;


namespace DatabaseManager.Client.Helpers
{
    public class Documentation: IDocumentation
    {
        private readonly IHttpService httpService;
        private string url = "api/documentation";

        public Documentation(IHttpService httpService)
        {
            this.httpService = httpService;
        }

        public async Task<string> GetDoc(string docName)
        {
            var response = await httpService.Get<string>($"{url}/{docName}");
            if (!response.Success)
            {
                throw new ApplicationException(await response.GetBody());
            }
            return response.Response;
        }
    }
}
