using DatabaseManager.Shared;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace DatabaseManager.Client.Helpers
{
    public interface ICookies
    {
        Task CreateCookie(CookieParameters cookieParams);
        Task DeleteCookie();
        Task<CookieParameters> GetCookie();
    }
}
