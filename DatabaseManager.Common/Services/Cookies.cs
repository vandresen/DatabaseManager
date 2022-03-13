using DatabaseManager.Shared;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DatabaseManager.Common.Services
{
    public class Cookies : ICookies
    {
        private readonly IHttpService httpService;
        private string url = "api/cookies";

        public Cookies(IHttpService httpService)
        {
            this.httpService = httpService;
        }

        public async Task<CookieParameters> GetCookie()
        {
            var response = await httpService.Get<CookieParameters>(url);
            if (!response.Success)
            {
                throw new ApplicationException(await response.GetBody());
            }
            return response.Response;
        }

        public async Task CreateCookie(CookieParameters cookieParams)
        {
            var response = await httpService.Post(url, cookieParams);
            if (!response.Success)
            {
                throw new ApplicationException(await response.GetBody());
            }
        }

        public async Task DeleteCookie()
        {
            var response = await httpService.Delete($"{url}");
            if (!response.Success)
            {
                throw new ApplicationException(await response.GetBody());
            }
        }
    }
}
