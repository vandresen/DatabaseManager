using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Net.Http;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using AutoMapper;
using DatabaseManager.Common.Helpers;
using DatabaseManager.Server.Entities;
using DatabaseManager.Server.Services;
using DatabaseManager.Shared;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace DatabaseManager.Server.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class DataQCController : ControllerBase
    {
        private string connectionString;
        private readonly string container = "sources";
        private readonly IFileStorageService fileStorageService;
        private readonly ITableStorageService tableStorageService;
        private readonly IMapper mapper;
        private readonly IWebHostEnvironment _env;
        private List<DataAccessDef> _accessDefs;

        public DataQCController(IConfiguration configuration,
            IFileStorageService fileStorageService,
            ITableStorageService tableStorageService,
            IMapper mapper,
            IWebHostEnvironment env)
        {
            connectionString = configuration.GetConnectionString("AzureStorageConnection");
            this.fileStorageService = fileStorageService;
            this.tableStorageService = tableStorageService;
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
            try
            {
                if (qcParams == null) return BadRequest();
                string tmpConnString = Request.Headers["AzureStorageConnection"];
                DataQC qc = new DataQC(tmpConnString);
                await qc.ProcessQcRule(qcParams);
            }
            catch (Exception ex)
            {
                return BadRequest(ex.ToString());
            }

            return Ok($"OK");
        }
    }
}
