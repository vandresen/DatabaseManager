using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using AutoMapper;
using DatabaseManager.Common.Helpers;
using DatabaseManager.Server.Entities;
using DatabaseManager.Shared;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json;

namespace DatabaseManager.Server.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class DataQCController : ControllerBase
    {
        private string connectionString;
        private readonly string container = "sources";
        private readonly IMapper mapper;
        private readonly IWebHostEnvironment _env;
        private List<DataAccessDef> _accessDefs;

        public DataQCController(IConfiguration configuration,
            IMapper mapper,
            IWebHostEnvironment env)
        {
            connectionString = configuration.GetConnectionString("AzureStorageConnection");
            this.mapper = mapper;
            _env = env;
        }

        [HttpGet("{source}")]
        public async Task<ActionResult<List<QcResult>>> Get(string source)
        {
            List<QcResult> qcResults = new List<QcResult>();
            try
            {
                string tmpConnString = Request.Headers["AzureStorageConnection"];
                DataQC qc = new DataQC(tmpConnString);
                DataQCParameters qcParms = new DataQCParameters();
                qcParms.DataConnector = source;
                qcResults = await qc.GetQCRules(qcParms);
            }
            catch (Exception ex)
            {
                return BadRequest($"{ex}");
            }
            return qcResults;
        }

        [HttpGet("{source}/{id}")]
        public async Task<ActionResult<string>> GetFailures(string source, int id)
        {
            string result = "[]";
            try
            {
                string tmpConnString = Request.Headers["AzureStorageConnection"];
                DataQC qc = new DataQC(tmpConnString);
                result = await qc.GetQCFailures(source, id);
            }
            catch (Exception)
            {
                return BadRequest();
            }
            return result;
        }

        /// <summary>
        /// Will clear the QC flags in the index for the selected source
        /// </summary>
        /// <param name="source">Name of the source connector</param>
        /// <returns></returns>
        [HttpPost("ClearQCFlags/{source}")]
        public async Task<ActionResult<string>> ClearQCFlags(string source)
        {
            if ( String.IsNullOrEmpty(source)) return BadRequest();

            try
            {
                string tmpConnString = Request.Headers["AzureStorageConnection"];
                DataQC qc = new DataQC(tmpConnString);
                await qc.ClearQCFlags(source);
            }
            catch (Exception ex)
            {
                return BadRequest(ex.ToString());
            }
            
            return Ok($"OK");
        }

        /// <summary>
        /// Will execute a QC rule based on the parameters settings
        /// </summary>
        /// <param name="qcParams"></param>
        /// <returns></returns>
        [HttpPost]
        public async Task<ActionResult<string>> ExecuteRule(DataQCParameters qcParams)
        {
            string result = "[]";
            try
            {
                if (qcParams == null) return BadRequest();
                string tmpConnString = Request.Headers["AzureStorageConnection"];
                DataQC qc = new DataQC(tmpConnString);
                List<int> failedObjects = new List<int>();
                failedObjects = await qc.ExecuteQcRule(qcParams);
                RuleFailures ruleFailures = new RuleFailures() { RuleId=qcParams.RuleId, Failures= failedObjects };
                result = JsonConvert.SerializeObject(ruleFailures);
            }
            catch (Exception ex)
            {
                return BadRequest(ex.ToString());
            }

            return result;
        }

        [HttpPost("Close/{source}")]
        public async Task<ActionResult<string>> CloseRulesExecution(string source, DataQCParameters qcParams)
        {
            try
            {
                if (qcParams == null) return BadRequest();
                string tmpConnString = Request.Headers["AzureStorageConnection"];
                DataQC qc = new DataQC(tmpConnString);
                await qc.CloseDataQC(source, qcParams.Failures);
            }
            catch (Exception ex)
            {
                return BadRequest(ex.ToString());
            }

            return Ok($"OK");
        }
    }
}
