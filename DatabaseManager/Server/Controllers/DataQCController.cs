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
using DatabaseManager.Server.Extensions;
using DatabaseManager.Server.Helpers;
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
            Helpers.DbUtilities dbConn = new Helpers.DbUtilities();
            string result = "[]";
            try
            {
                string tmpConnString = Request.Headers["AzureStorageConnection"];
                fileStorageService.SetConnectionString(tmpConnString);
                tableStorageService.SetConnectionString(tmpConnString);
                SourceEntity entity = await tableStorageService.GetTableRecord<SourceEntity>(container, source);
                ConnectParameters connector = mapper.Map<ConnectParameters>(entity);
                dbConn.OpenConnection(connector);
                string accessJson = await fileStorageService.ReadFile("connectdefinition", "PPDMDataAccess.json");
                _accessDefs = JsonConvert.DeserializeObject<List<DataAccessDef>>(accessJson);
                RuleModel rule = GetRule(dbConn, id);
                result = GetFailedObjects(dbConn, rule.RuleKey);
            }
            catch (Exception)
            {
                return BadRequest();
            }
       
            dbConn.CloseConnection();
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

        private RuleModel GetRule(Helpers.DbUtilities dbConn, int id)
        {
            List<RuleModel> rules = new List<RuleModel>();
            DataAccessDef ruleAccessDef = _accessDefs.First(x => x.DataType == "Rules");
            string sql = ruleAccessDef.Select;
            string query = $" where Id = {id}";
            DataTable dt = dbConn.GetDataTable(sql, query);
            string jsonString = JsonConvert.SerializeObject(dt);
            rules = JsonConvert.DeserializeObject<List<RuleModel>>(jsonString);
            RuleModel rule = rules.First();

            DataAccessDef functionAccessDef = _accessDefs.First(x => x.DataType == "Functions");
            sql = functionAccessDef.Select;
            query = $" where FunctionName = '{rule.RuleFunction}'";
            dt = dbConn.GetDataTable(sql, query);

            string functionURL = dt.Rows[0]["FunctionUrl"].ToString();
            string functionKey = dt.Rows[0]["FunctionKey"].ToString();
            if (!string.IsNullOrEmpty(functionKey)) functionKey = "?code=" + functionKey;
            rule.RuleFunction = functionURL + functionKey;
            return rule;
        }

        private string GetFailedObjects(Helpers.DbUtilities dbConn, string ruleKey)
        {
            List<DmsIndex> qcIndex = new List<DmsIndex>();
            DataAccessDef ruleAccessDef = _accessDefs.First(x => x.DataType == "Index");
            string sql = ruleAccessDef.Select;
            string query = $" where QC_STRING like '%{ruleKey};%'";
            DataTable idx = dbConn.GetDataTable(sql, query);
            foreach (DataRow idxRow in idx.Rows)
            {
                string dataType = idxRow["DATATYPE"].ToString();
                string indexId = idxRow["INDEXID"].ToString();
                string jsonData = idxRow["JSONDATAOBJECT"].ToString();
                int intIndexId = Convert.ToInt32(indexId);
                qcIndex.Add(new DmsIndex()
                {
                    Id = intIndexId,
                    DataType = dataType,
                    JsonData = jsonData
                });
            }
            string result = JsonConvert.SerializeObject(qcIndex);

            return result;
        }
    }
}
