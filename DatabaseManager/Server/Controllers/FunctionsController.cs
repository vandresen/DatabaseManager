using System;
using Microsoft.Extensions.Configuration;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using DatabaseManager.Shared;
using Microsoft.AspNetCore.Hosting;
using DatabaseManager.Server.Services;
using AutoMapper;
using DatabaseManager.Common.Helpers;

namespace DatabaseManager.Server.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class FunctionsController : ControllerBase
    {
        private string connectionString;
        private readonly string container = "sources";
        private readonly IMapper mapper;
        private readonly IWebHostEnvironment _env;

        public FunctionsController(IConfiguration configuration,
            IMapper mapper,
            IWebHostEnvironment env)
        {
            connectionString = configuration.GetConnectionString("AzureStorageConnection");
            this.mapper = mapper;
            _env = env;
        }

        [HttpGet("{source}")]
        public async Task<ActionResult<string>> Get(string source)
        {
            string result = "";
            try
            {
                GetStorageAccount();
                RuleManagement rm = new RuleManagement(connectionString);
                result = await rm.GetFunctions(source);
            }
            catch (Exception)
            {
                return BadRequest();
            }
            return result;
        }

        [HttpGet("{source}/{id:int}")]
        public async Task<ActionResult<string>> GetFunction(string source, int id)
        {
            string result = "";
            try
            {
                GetStorageAccount();
                RuleManagement rm = new RuleManagement(connectionString);
                result = await rm.GetFunction(source, id);
            }
            catch (Exception)
            {
                return BadRequest();
            }
            return result;
        }

        [HttpPost("{source}")]
        public async Task<ActionResult<string>> SaveFunction(string source, RuleFunctions function)
        {
            if (function == null) return BadRequest();
            try
            {
                GetStorageAccount();
                RuleManagement rm = new RuleManagement(connectionString);
                await rm.SaveFunction(source, function);
            }
            catch (Exception)
            {
                return BadRequest();
            }
            return Ok($"OK");
        }

        [HttpPut("{source}/{id:int}")]
        public async Task<ActionResult<string>> UpdateFunction(string source, int id, RuleFunctions function)
        {
            if (function == null) return BadRequest();
            try
            {
                GetStorageAccount();
                RuleManagement rm = new RuleManagement(connectionString);
                await rm.UpdateFunction(source, id, function);
            }
            catch (Exception)
            {
                return BadRequest();
            }
            return Ok($"OK");
        }

        [HttpDelete("{source}/{id}")]
        public async Task<ActionResult> Delete(string source, int id)
        {
            try
            {
                GetStorageAccount();
                RuleManagement rm = new RuleManagement(connectionString);
                await rm.DeleteFunction(source, id);
            }
            catch (Exception)
            {
                return BadRequest();
            }
            return NoContent();
        }

        private void GetStorageAccount()
        {
            string tmpConnString = Request.Headers["AzureStorageConnection"];
            if (!string.IsNullOrEmpty(tmpConnString)) connectionString = tmpConnString;
            if (string.IsNullOrEmpty(connectionString))
            {
                Exception error = new Exception($"Azure storage key string is not set");
                throw error;
            }
        }
    }
}
