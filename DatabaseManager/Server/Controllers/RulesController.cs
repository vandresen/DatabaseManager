using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Threading.Tasks;
using AutoMapper;
using DatabaseManager.Server.Entities;
using DatabaseManager.Server.Helpers;
using DatabaseManager.Server.Services;
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
    public class RulesController : ControllerBase
    {
        private string connectionString;
        private readonly string container = "sources";
        private readonly string ruleShare = "rules";
        private readonly string predictionContainer = "predictions";
        private readonly IFileStorageService fileStorageService;
        private readonly ITableStorageService tableStorageService;
        private readonly IMapper mapper;
        private readonly IWebHostEnvironment _env;

        public RulesController(IConfiguration configuration,
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
        public async Task<ActionResult<string>> Get(string source)
        {
            SetStorageAccount();
            string accessJson = await fileStorageService.ReadFile("connectdefinition", "PPDMDataAccess.json");
            List<DataAccessDef> accessDefs = JsonConvert.DeserializeObject<List<DataAccessDef>>(accessJson);
            DataAccessDef ruleAccessDef = accessDefs.First(x => x.DataType == "Rules");
            string result = "";
            DbUtilities dbConn = new DbUtilities();
            try
            {
                SourceEntity entity = await tableStorageService.GetTableRecord<SourceEntity>(container, source);
                ConnectParameters connector = mapper.Map<ConnectParameters>(entity);
                if (connector == null) return BadRequest();
                dbConn.OpenConnection(connector);
                string select = ruleAccessDef.Select;
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
            SetStorageAccount();
            string accessJson = await fileStorageService.ReadFile("connectdefinition", "PPDMDataAccess.json");
            List<DataAccessDef> accessDefs = JsonConvert.DeserializeObject<List<DataAccessDef>>(accessJson);
            DataAccessDef ruleAccessDef = accessDefs.First(x => x.DataType == "Rules");
            string result = "";
            DbUtilities dbConn = new DbUtilities();
            try
            {
                SourceEntity entity = await tableStorageService.GetTableRecord<SourceEntity>(container, source);
                ConnectParameters connector = mapper.Map<ConnectParameters>(entity);
                if (connector == null) return BadRequest();
                dbConn.OpenConnection(connector);
                string select = ruleAccessDef.Select;
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
            try
            {
                SetStorageAccount();
                List<PredictionEntity> predictionEntities = await tableStorageService.GetTableRecords<PredictionEntity>(predictionContainer);
                List<PredictionSet> predictionSets = new List<PredictionSet>();
                foreach (PredictionEntity entity in predictionEntities)
                {
                    predictionSets.Add(new PredictionSet()
                    {
                        Name = entity.RowKey,
                        Description = entity.Decsription
                    });
                }
                return predictionSets;
            }
            catch (Exception)
            {
                return BadRequest();
            }
        }

        [HttpGet("RuleFile/{rulename}")]
        public async Task<ActionResult<string>> GetPrediction(string ruleName)
        {
            try
            {
                SetStorageAccount();
                ruleName = ruleName + ".json";
                string result = await fileStorageService.ReadFile(ruleShare, ruleName);
                if (string.IsNullOrEmpty(result))
                {
                    Exception error = new Exception($"Empty data from {ruleName}");
                    throw error;
                }
                return result;
            }
            catch (Exception ex)
            {
                return BadRequest(ex.ToString());
            }
            
        }

        [HttpGet("RuleInfo/{source}")]
        public async Task<ActionResult<RuleInfo>> GetRuleInfo(string source)
        {
            SetStorageAccount();
            string accessJson = await fileStorageService.ReadFile("connectdefinition", "PPDMDataAccess.json");
            List<DataAccessDef> accessDefs = JsonConvert.DeserializeObject<List<DataAccessDef>>(accessJson);
            RuleInfo ruleInfo = new RuleInfo();
            ruleInfo.DataTypeOptions = new List<string>();
            ruleInfo.DataAttributes = new Dictionary<string, string>();
            foreach (DataAccessDef accessDef in accessDefs)
            {
                ruleInfo.DataTypeOptions.Add(accessDef.DataType);
                string[] attributeArray = Common.GetAttributes(accessDef.Select);
                string attributes = String.Join(",", attributeArray);
                ruleInfo.DataAttributes.Add(accessDef.DataType, attributes);
            }
            return ruleInfo;
        }

        [HttpPost("{source}")]
        public async Task<ActionResult<string>> SaveRule(string source, RuleModel rule)
        {
            SetStorageAccount();
            string accessJson = await fileStorageService.ReadFile("connectdefinition", "PPDMDataAccess.json");
            List<DataAccessDef> accessDefs = JsonConvert.DeserializeObject<List<DataAccessDef>>(accessJson);
            DataAccessDef ruleAccessDef = accessDefs.First(x => x.DataType == "Rules");
            if (rule == null) return BadRequest();
            DbUtilities dbConn = new DbUtilities();
            try
            {
                SourceEntity entity = await tableStorageService.GetTableRecord<SourceEntity>(container, source);
                ConnectParameters connector = mapper.Map<ConnectParameters>(entity);
                if (connector == null) return BadRequest();
                dbConn.OpenConnection(connector);
                RuleUtilities.SaveRule(dbConn, rule, ruleAccessDef);
            }
            catch (Exception ex)
            {
                return BadRequest();
            }
            dbConn.CloseConnection();
            return Ok($"OK");
        }

        [HttpPost("RuleFile/{rulename}")]
        public async Task<ActionResult<string>> SaveRuleToFile(string RuleName, PredictionSet predictionSet)
        {
            List<RuleModel> rules = predictionSet.RuleSet;
            if (rules == null) return BadRequest();
            try
            {
                SetStorageAccount();
                PredictionEntity tmpEntity = await tableStorageService.GetTableRecord<PredictionEntity>(predictionContainer, RuleName);
                if (tmpEntity != null)
                {
                    return BadRequest("Prediction set exist");
                }

                string fileName = RuleName + ".json";
                string json = JsonConvert.SerializeObject(rules, Formatting.Indented);
                string url = await fileStorageService.SaveFileUri(ruleShare, fileName, json);
                PredictionEntity predictionEntity = new PredictionEntity(RuleName)
                {
                    RuleUrl = url,
                    Decsription = predictionSet.Description
                };
                await tableStorageService.SaveTableRecord(predictionContainer, RuleName, predictionEntity);
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
            DbUtilities dbConn = new DbUtilities();
            try
            {
                SetStorageAccount();
                SourceEntity entity = await tableStorageService.GetTableRecord<SourceEntity>(container, source);
                ConnectParameters connector = mapper.Map<ConnectParameters>(entity);
                if (connector == null) return BadRequest();
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
            try
            {
                SetStorageAccount();
                DbUtilities dbConn = new DbUtilities();
                SourceEntity entity = await tableStorageService.GetTableRecord<SourceEntity>(container, source);
                ConnectParameters connector = mapper.Map<ConnectParameters>(entity);
                if (connector == null) return BadRequest();
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
                dbConn.CloseConnection();
            }
            catch (Exception)
            {
                return BadRequest();
            }

            return NoContent();
        }

        /// <summary>
        /// Delete a prediction set. Will delete the entry in prediction set table and the actual json rule file
        /// </summary>
        /// <param name="RuleName"></param>
        /// <returns></returns>
        [HttpDelete("RuleFile/{rulename}")]
        public async Task<ActionResult> DeleteTable(string RuleName)
        {
            try
            {
                SetStorageAccount();
                await tableStorageService.DeleteTable(predictionContainer, RuleName);
                string ruleFile = RuleName + ".json";
                await fileStorageService.DeleteFile(ruleShare, ruleFile);
            }
            catch (Exception ex)
            {
                return BadRequest();
            }
            
            return NoContent();
        }

        private void SetStorageAccount()
        {
            string tmpConnString = Request.Headers["AzureStorageConnection"];
            fileStorageService.SetConnectionString(tmpConnString);
            tableStorageService.SetConnectionString(tmpConnString);
        }
    }
}
