using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using DatabaseManager.Shared;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace DatabaseManager.Server.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class CookiesController : ControllerBase
    {
        private string key = "PDOStorageAccount";

        [HttpGet]
        public async Task<ActionResult<CookieParameters>> Get()
        {
            CookieParameters cookieParams = new CookieParameters();
            try
            {
                cookieParams.Value = Request.Cookies[key];
            }
            catch (Exception ex)
            {
                return BadRequest(ex.ToString());
            }
            return cookieParams;
        }

        [HttpPost]
        public async Task<ActionResult<string>> Create(CookieParameters cookieParams)
        {
            try
            {
                int expires = cookieParams.ExpirationDays;
                if (expires < 1) expires = 1;
                CookieOptions cookieOptions = new CookieOptions();
                cookieOptions.Expires = DateTime.Now.AddDays(expires);
                Response.Cookies.Append(key, cookieParams.Value, cookieOptions);
            }
            catch (Exception ex)
            {
                return BadRequest(ex.ToString());
            }

            return Ok($"OK");
        }

        [HttpDelete]
        public async Task<ActionResult> Delete()
        {
            try
            {
                string value = "";
                CookieOptions cookieOptions = new CookieOptions();
                cookieOptions.Expires = DateTime.Now.AddDays(-1);
                Response.Cookies.Append(key, value, cookieOptions);
            }
            catch (Exception ex)
            {
                return BadRequest(ex.ToString());
            }

            return NoContent();
        }
    }
}
