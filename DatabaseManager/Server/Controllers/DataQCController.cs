﻿using System;
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
        private string _connectionString;
        private readonly string container = "sources";
        private readonly IMapper mapper;
        private readonly IWebHostEnvironment _env;
        private List<DataAccessDef> _accessDefs;

        public DataQCController(IConfiguration configuration,
            IMapper mapper,
            IWebHostEnvironment env)
        {
            _connectionString = configuration.GetConnectionString("AzureStorageConnection");
            this.mapper = mapper;
            _env = env;
        }

        [HttpGet("{source}")]
        public async Task<ActionResult<List<QcResult>>> Get(string source)
        {
            List<QcResult> results = new List<QcResult>();
            try
            {
                string tmpConnString = Request.Headers["AzureStorageConnection"];
                DataQC qc = new DataQC(tmpConnString);
                DataQCParameters qcParms = new DataQCParameters();
                qcParms.DataConnector = source;
                results = await qc.GetResults(source);
            }
            catch (Exception ex)
            {
                return BadRequest($"{ex}");
            }
            return results;
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

        [HttpPost("ClearQCFlags/{source}")]
        public async Task<ActionResult<string>> ClearQCFlags(string source)
        {
            if (String.IsNullOrEmpty(source)) return BadRequest();

            try
            {
                GetStorageAccount();
                DataQC dq = new DataQC(_connectionString);
                await dq.ClearQCFlags(source);
            }
            catch (Exception ex)
            {
                return BadRequest(ex.ToString());
            }

            return Ok($"OK");
        }

        [HttpPost]
        public async Task<ActionResult<string>> ExecuteRule(DataQCParameters qcParams)
        {
            try
            {
                List<int> result = new List<int>();
                if (qcParams == null) return BadRequest();
                GetStorageAccount();
                DataQC dq = new DataQC(_connectionString);
                result = await dq.ExecuteQcRule(qcParams);
                //fileStorageService.SetConnectionString(tmpConnString);
                //tableStorageService.SetConnectionString(tmpConnString);
                //string jsonConnectDef = await fileStorageService.ReadFile("connectdefinition", "PPDMDataAccess.json");
                //_accessDefs = JsonConvert.DeserializeObject<List<DataAccessDef>>(jsonConnectDef);
                //SourceEntity entity = await tableStorageService.GetTableRecord<SourceEntity>(container, qcParams.DataConnector);
                //ConnectParameters connector = mapper.Map<ConnectParameters>(entity);
                //DbUtilities dbConn = new DbUtilities();
                //dbConn.OpenConnection(connector);

                //RuleModel rule = GetRule(dbConn, qcParams.RuleId);

                //manageQCFlags = new ManageIndexTable(_accessDefs, connector.ConnectionString, rule.DataType);

                //if (qcParams.ClearQCFlags)
                //{
                //    manageQCFlags.ClearQCFlags(qcParams.ClearQCFlags);
                //}

                //manageQCFlags.InitQCFlags(qcParams.ClearQCFlags);
                //await QualityCheckDataType(dbConn, rule, connector);
                //dbConn.CloseConnection();
                //manageQCFlags.SaveQCFlags();
            }
            catch (Exception ex)
            {
                return BadRequest(ex.ToString());
            }

            return Ok($"OK");
        }

        private void GetStorageAccount()
        {
            string tmpConnString = Request.Headers["AzureStorageConnection"];
            if (!string.IsNullOrEmpty(tmpConnString)) _connectionString = tmpConnString;
            if (string.IsNullOrEmpty(_connectionString))
            {
                Exception error = new Exception($"Azure storage key string is not set");
                throw error;
            }
        }
    }
}
