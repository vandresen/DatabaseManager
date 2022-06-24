using DatabaseManager.Common.Helpers;
using DatabaseManager.Shared;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace DatabaseManager.Server.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ReportEditController : ControllerBase
    {
        private string connectionString;

        public ReportEditController()
        {

        }

        [HttpGet("{source}/{datatype}")]
        public async Task<ActionResult<string>> GetAttributeInfo(string source, string datatype)
        {
            GetStorageAccount();
            AttributeInfo info = new AttributeInfo();
            ReportEditManagement rm = new ReportEditManagement(connectionString);
            string responseMessage = await rm.GetAttributeInfo(source, datatype);
            return responseMessage;
        }

        [HttpPut("{source}")]
        public async Task<ActionResult<string>> UpdateIndex(string source,  ReportData reportData)
        {
            try
            {
                GetStorageAccount();
                ReportEditManagement rm = new ReportEditManagement(connectionString);
                await rm.InsertEdits(reportData, source);
            }
            catch (Exception ex)
            {
                return BadRequest();
            }
            return Ok($"OK");
        }

        [HttpDelete("{source}/{id}")]
        public async Task<ActionResult> Delete(string source, int id, string rulekey)
        {
            try
            {
                GetStorageAccount();
                ReportEditManagement rm = new ReportEditManagement(connectionString);
                await rm.DeleteEdits(source, id);
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
