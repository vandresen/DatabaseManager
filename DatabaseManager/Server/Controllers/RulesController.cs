using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Threading.Tasks;
using AutoMapper;
using DatabaseManager.Common.Helpers;
using DatabaseManager.Server.Entities;
using DatabaseManager.Server.Helpers;
using DatabaseManager.Server.Services;
using DatabaseManager.Shared;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json;
using DbUtilities = DatabaseManager.Common.Helpers.DbUtilities;

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
            string result = "";
            try
            {
                GetStorageAccount();
                RuleManagement rules = new RuleManagement(connectionString);
                result= await rules.GetRules(source);
            }
            catch (Exception)
            {
                return BadRequest();
            }
            return result;
        }

        [HttpGet("{source}/{id:int}")]
        public async Task<ActionResult<string>> GetRule(string source, int id)
        {
            string result = "";
            try
            {
                GetStorageAccount();
                RuleManagement rules = new RuleManagement(connectionString);
                result = await rules.GetRule(source, id);
            }
            catch (Exception)
            {
                return BadRequest();
            }
            return result;
        }

        [HttpGet("RuleFile")]
        public async Task<ActionResult<List<PredictionSet>>> GetPredictions()
        {
            try
            {
                List<PredictionSet> predictionSets = new List<PredictionSet>();
                GetStorageAccount();
                RuleManagement rules = new RuleManagement(connectionString);
                string responseMessage = await rules.GetPredictions();
                predictionSets = JsonConvert.DeserializeObject<List<PredictionSet>>(responseMessage);
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
                GetStorageAccount();
                RuleManagement rules = new RuleManagement(connectionString);
                string result = await rules.GetPrediction(ruleName);
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
            GetStorageAccount();
            RuleManagement rules = new RuleManagement(connectionString);
            string responseMessage = await rules.GetRuleInfo();
            RuleInfo ruleInfo = JsonConvert.DeserializeObject<RuleInfo>(responseMessage);
            return ruleInfo;
        }

        [HttpPost("{source}")]
        public async Task<ActionResult<string>> SaveRule(string source, RuleModel rule)
        {
            try
            {
                GetStorageAccount();
                RuleManagement rules = new RuleManagement(connectionString);
                await rules.SaveRule(source, rule);
            }
            catch (Exception ex)
            {
                return BadRequest();
            }
            return Ok($"OK");
        }

        [HttpPost("RuleFile/{rulename}")]
        public async Task<ActionResult<string>> SaveRuleToFile(string RuleName, PredictionSet predictionSet)
        {
            try
            {
                GetStorageAccount();
                RuleManagement rules = new RuleManagement(connectionString);
                await rules.SavePredictionSet(RuleName, predictionSet);
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
            try
            {
                GetStorageAccount();
                RuleManagement rules = new RuleManagement(connectionString);
                await rules.UpdateRule(source, id, rule);
            }
            catch (Exception ex)
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
                RuleManagement rules = new RuleManagement(connectionString);
                await rules.DeleteRule(source, id);
                //SetStorageAccount();
                //DbUtilities dbConn = new DbUtilities();
                //SourceEntity entity = await tableStorageService.GetTableRecord<SourceEntity>(container, source);
                //ConnectParameters connector = mapper.Map<ConnectParameters>(entity);
                //if (connector == null) return BadRequest();
                //dbConn.OpenConnection(connector);
                //string select = "Select * from pdo_qc_rules ";
                //string query = $"where Id = {id}";
                //DataTable dt = dbConn.GetDataTable(select, query);
                //if (dt.Rows.Count == 1)
                //{
                //    string table = "pdo_qc_rules";
                //    dbConn.DBDelete(table, query);
                //}
                //else
                //{
                //    return BadRequest();
                //}
                //dbConn.CloseConnection();
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
                GetStorageAccount();
                RuleManagement rules = new RuleManagement(connectionString);
                await rules.DeletePrediction(RuleName);

                //SetStorageAccount();
                //await tableStorageService.DeleteTable(predictionContainer, RuleName);
                //string ruleFile = RuleName + ".json";
                //await fileStorageService.DeleteFile(ruleShare, ruleFile);
            }
            catch (Exception ex)
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

        private void SetStorageAccount()
        {
            string tmpConnString = Request.Headers["AzureStorageConnection"];
            fileStorageService.SetConnectionString(tmpConnString);
            tableStorageService.SetConnectionString(tmpConnString);
        }
    }
}
