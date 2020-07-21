﻿using System;
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
using Microsoft.Azure.Storage;
using Microsoft.Azure.Storage.File;
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json;

namespace DatabaseManager.Server.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class RulesController : ControllerBase
    {
        private readonly string connectionString;
        private readonly string container = "sources";
        private readonly string ruleShare = "rules";
        private readonly IWebHostEnvironment _env;
        List<DataAccessDef> _accessDefs;
        DataAccessDef _ruleAccessDef;

        public RulesController(IConfiguration configuration, IWebHostEnvironment env)
        {
            connectionString = configuration.GetConnectionString("AzureStorageConnection");
            _env = env;
            _accessDefs = Common.GetDataAccessDefinition(_env);
            _ruleAccessDef = _accessDefs.First(x => x.DataType == "Rules");
        }

        [HttpGet("{source}")]
        public async Task<ActionResult<string>> Get(string source)
        {
            ConnectParameters connector = Common.GetConnectParameters(connectionString, container, source);
            if (connector == null) return BadRequest();
            string result = "";
            DbUtilities dbConn = new DbUtilities();
            try
            {
                dbConn.OpenConnection(connector);
                string select = _ruleAccessDef.Select;
                string query = "";
                DataTable dt = dbConn.GetDataTable(select, query);
                result = JsonConvert.SerializeObject(dt, Formatting.Indented);
            }
            catch (Exception)
            {
                return BadRequest();
            }
            
            dbConn.CloseConnection();
            return result;
        }

        [HttpGet("{source}/{id:int}")]
        public async Task<ActionResult<string>> GetRule(string source, int id)
        {
            ConnectParameters connector = Common.GetConnectParameters(connectionString, container, source);
            if (connector == null) return BadRequest();
            string result = "";
            DbUtilities dbConn = new DbUtilities();
            try
            {
                dbConn.OpenConnection(connector);
                string select = _ruleAccessDef.Select;
                string query = $" where Id = {id}";
                DataTable dt = dbConn.GetDataTable(select, query);
                result = JsonConvert.SerializeObject(dt, Formatting.Indented);
                result = result.Replace("[", "");
                result = result.Replace("]", "");
            }
            catch (Exception)
            {
                return BadRequest();
            }

            dbConn.CloseConnection();
            return result;
        }

        [HttpGet("RuleFile")]
        public async Task<ActionResult<List<PredictionSet>>> GetPredictions()
        {
            List<PredictionSet> files = new List<PredictionSet>();
            try
            {
                CloudStorageAccount account = CloudStorageAccount.Parse(connectionString);
                CloudFileClient fileClient = account.CreateCloudFileClient();
                CloudFileShare share = account.CreateCloudFileClient().GetShareReference(ruleShare);
                IEnumerable<IListFileItem> fileList = share.GetRootDirectoryReference().ListFilesAndDirectories();
                foreach (IListFileItem listItem in fileList)
                {
                    if (listItem.GetType() == typeof(CloudFile))
                    {
                        files.Add(new PredictionSet()
                        {
                            Name = listItem.Uri.Segments.Last()
                        });
                        //files.Add(listItem.Uri.Segments.Last());
                    }
                }
            }
            catch (Exception)
            {
                return BadRequest();
            }
            return files;
        }

        [HttpGet("RuleFile/{rulename}")]
        public async Task<ActionResult<string>> GetPrediction(string ruleName)
        {
            string result = "";
            try
            {
                CloudStorageAccount account = CloudStorageAccount.Parse(connectionString);
                CloudFileClient fileClient = account.CreateCloudFileClient();
                CloudFileShare share = fileClient.GetShareReference(ruleShare);
                if (share.Exists())
                {
                    CloudFileDirectory rootDir = share.GetRootDirectoryReference();
                    CloudFile file = rootDir.GetFileReference(ruleName);
                    if (file.Exists())
                    {
                        result = file.DownloadTextAsync().Result;
                    }
                    else
                    {
                        return BadRequest();
                    }
                }
                else
                {
                    return BadRequest();
                }
            }
            catch (Exception)
            {
                return BadRequest();
            }
            return result;
        }

        [HttpGet("RuleInfo/{source}")]
        public async Task<ActionResult<RuleInfo>> GetRuleInfo(string source)
        {
            ConnectParameters connector = Common.GetConnectParameters(connectionString, container, source);
            if (connector == null) return BadRequest();
            RuleInfo ruleInfo = new RuleInfo();
            ruleInfo.DataTypeOptions = new List<string> { "WellBore", "Log", "MarkerPick" };
            ruleInfo.DataAttributes = new Dictionary<string, string> {
            { "WellBore", "UWI, FINAL_TD, WELL_NAME, SURFACE_LATITUDE, SURFACE_LONGITUDE," +
            "LEASE_NAME, DEPTH_DATUM_ELEV, DEPTH_DATUM, OPERATOR, ASSIGNED_FIELD, CURRENT_STATUS," +
            "GROUND_ELEV, REMARK, ROW_CHANGED_DATE, ROW_CHANGED_BY" +
            " from WELL" },
            { "MarkerPick", "STRAT_NAME_SET_ID, STRAT_UNIT_ID, UWI, INTERP_ID, DOMINANT_LITHOLOGY, PICK_DEPTH," +
            "REMARK, ROW_CHANGED_DATE, ROW_CHANGED_BY " +
            " from STRAT_WELL_SECTION" },
            { "Log", "UWI, CURVE_ID, NULL_REPRESENTATION, VALUE_COUNT, MAX_INDEX, MIN_INDEX, ROW_CHANGED_DATE, ROW_CHANGED_BY from well_log_curve"}
        };
            return ruleInfo;
        }

        [HttpPost("{source}")]
        public async Task<ActionResult<string>> SaveRule(string source, RuleModel rule)
        {
            if (rule == null) return BadRequest();
            ConnectParameters connector = Common.GetConnectParameters(connectionString, container, source);
            if (connector == null) return BadRequest();
            DbUtilities dbConn = new DbUtilities();
            try
            {
                dbConn.OpenConnection(connector);
                RuleUtilities.SaveRule(dbConn, rule, _ruleAccessDef);
            }
            catch (Exception ex)
            {
                return BadRequest();
            }
            dbConn.CloseConnection();
            return Ok($"OK");
        }

        [HttpPost("RuleFile/{rulename}")]
        public async Task<ActionResult<string>> SaveRuleToFile(string RuleName, List<RuleModel> rules)
        {
            if (rules == null) return BadRequest();
            try
            {
                CloudStorageAccount account = CloudStorageAccount.Parse(connectionString);
                CloudFileClient fileClient = account.CreateCloudFileClient();
                CloudFileShare share = fileClient.GetShareReference(ruleShare);
                if (!share.Exists())
                {
                    share.Create();
                }
                CloudFileDirectory rootDir = share.GetRootDirectoryReference();
                string fileName = RuleName + ",json";
                string json = JsonConvert.SerializeObject(rules, Formatting.Indented);
                CloudFile file = rootDir.GetFileReference(fileName);
                if (!file.Exists())
                {
                    file.Create(json.Length);
                }
                file.UploadText(json);
            }
            catch (Exception ex)
            {
                return BadRequest();
            }
            return Ok($"OK");
        }

        [HttpPut("{source}/{id:int}")]
        public async Task<ActionResult<string>> UpdateRule(string source, int id, RuleModel rule)
        {
            if (rule == null) return BadRequest();
            ConnectParameters connector = Common.GetConnectParameters(connectionString, container, source);
            if (connector == null) return BadRequest();
            DbUtilities dbConn = new DbUtilities();
            try
            {
                dbConn.OpenConnection(connector);
                string select = "Select * from pdo_qc_rules ";
                string query = $"where Id = {id}";
                DataTable dt = dbConn.GetDataTable(select, query);
                if (dt.Rows.Count == 1)
                {
                    rule.Id = id;
                    RuleUtilities.UpdateRule(dbConn, rule);
                }
                else
                {
                    return BadRequest();
                }
                
            }
            catch (Exception ex)
            {
                return BadRequest();
            }
            dbConn.CloseConnection();
            return Ok($"OK");
        }

        [HttpDelete("{source}/{id}")]
        public async Task<ActionResult> Delete(string source, int id)
        {
            ConnectParameters connector = Common.GetConnectParameters(connectionString, container, source);
            if (connector == null) return BadRequest();
            DbUtilities dbConn = new DbUtilities();
            try
            {
                dbConn.OpenConnection(connector);
                string select = "Select * from pdo_qc_rules ";
                string query = $"where Id = {id}";
                DataTable dt = dbConn.GetDataTable(select, query);
                if (dt.Rows.Count == 1)
                {
                    string table = "pdo_qc_rules";
                    dbConn.DBDelete(table, query);
                }
                else
                {
                    return BadRequest();
                }
            }
            catch (Exception)
            {
                return BadRequest();
            }

            return NoContent();
        }
    }
}