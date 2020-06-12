using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Threading.Tasks;
using DatabaseManager.Server.Entities;
using DatabaseManager.Server.Helpers;
using DatabaseManager.Shared;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json;

namespace DatabaseManager.Server.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class DataQCController : ControllerBase
    {
        private readonly string connectionString;
        private readonly string container = "sources";
        private readonly IWebHostEnvironment _env;
        List<DataAccessDef> _accessDefs;
        DataAccessDef _indexAccessDef;

        public DataQCController(IConfiguration configuration, IWebHostEnvironment env)
        {
            connectionString = configuration.GetConnectionString("AzureStorageConnection");
            _env = env;
            _accessDefs = Common.GetDataAccessDefinition(_env);
            _indexAccessDef = _accessDefs.First(x => x.DataType == "Index");
        }

        [HttpGet("{source}")]
        public async Task<ActionResult<List<QcResult>>> Get(string source)
        {
            ConnectParameters connector = Common.GetConnectParameters(connectionString, container, source);
            if (connector == null) return BadRequest();
            DbUtilities dbConn = new DbUtilities();
            List<QcResult> qcResults = new List<QcResult>();
            try
            {
                dbConn.OpenConnection(connector);
                qcResults = GetQcResult(dbConn);
            }
            catch (Exception)
            {
                return BadRequest();
            }

            dbConn.CloseConnection();
            return qcResults;
        }

        private List<QcResult> GetQcResult(DbUtilities dbConn)
        {
            List<QcResult> qcResult = new List<QcResult>();
            DataAccessDef ruleAccessDef = _accessDefs.First(x => x.DataType == "Rules");
            string sql = ruleAccessDef.Select;
            string query = " where Active = 'Y' and RuleType != 'Predictions'";
            DataTable dt = dbConn.GetDataTable(sql, query);
            string jsonString = JsonConvert.SerializeObject(dt);
            qcResult = JsonConvert.DeserializeObject<List<QcResult>>(jsonString);
            foreach (QcResult qcItem in qcResult)
            {
                sql = _indexAccessDef.Select;
                query = $" where QC_STRING like '%{qcItem.RuleKey};%'";
                DataTable ft = dbConn.GetDataTable(sql, query);
                qcItem.Failures = ft.Rows.Count;
            }

            return qcResult;
        }
    }
}
