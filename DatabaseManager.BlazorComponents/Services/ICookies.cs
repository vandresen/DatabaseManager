using DatabaseManager.Shared;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DatabaseManager.BlazorComponents.Services
{
    public interface ICookies
    {
        Task CreateCookie(CookieParameters cookieParams);
        Task DeleteCookie();
        Task<CookieParameters> GetCookie();
    }
}
