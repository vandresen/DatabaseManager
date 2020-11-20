using System;
using System.Collections.Generic;
using Microsoft.Extensions.Configuration;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using DatabaseManager.Shared;
using DatabaseManager.Server.Helpers;
using Microsoft.AspNetCore.Hosting;
using DatabaseManager.Server.Entities;
using System.Data;
using Newtonsoft.Json;
using DatabaseManager.Server.Services;
using AutoMapper;

namespace DatabaseManager.Server.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class FunctionsController : ControllerBase
    {
        private string connectionString;
        private readonly string container = "sources";
        private readonly IFileStorageService fileStorageService;
        private readonly ITableStorageService tableStorageService;
        private readonly IMapper mapper;
        private readonly IWebHostEnvironment _env;

        public FunctionsController(IConfiguration configuration,
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
            string tmpConnString = Request.Headers["AzureStorageConnection"];
            tableStorageService.SetConnectionString(tmpConnString);
            fileStorageService.SetConnectionString(tmpConnString);
            string accessJson = await fileStorageService.ReadFile("connectdefinition", "PPDMDataAccess.json");
            List<DataAccessDef> accessDefs = JsonConvert.DeserializeObject<List<DataAccessDef>>(accessJson);
            DataAccessDef functionAccessDef = accessDefs.First(x => x.DataType == "Functions");
            SourceEntity entity = await tableStorageService.GetTableRecord<SourceEntity>(container, source);
            ConnectParameters connector = mapper.Map<ConnectParameters>(entity);
            string result = "";
            DbUtilities dbConn = new DbUtilities();
            try
            {
                dbConn.OpenConnection(connector);
                string select = functionAccessDef.Select;
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
        public async Task<ActionResult<string>> GetFunction(string source, int id)
        {
            string result = "";
            DbUtilities dbConn = new DbUtilities();
            try
            {
                string tmpConnString = Request.Headers["AzureStorageConnection"];
                tableStorageService.SetConnectionString(tmpConnString);
                fileStorageService.SetConnectionString(tmpConnString);
                string accessJson = await fileStorageService.ReadFile("connectdefinition", "PPDMDataAccess.json");
                List<DataAccessDef> accessDefs = JsonConvert.DeserializeObject<List<DataAccessDef>>(accessJson);
                DataAccessDef functionAccessDef = accessDefs.First(x => x.DataType == "Functions");
                SourceEntity entity = await tableStorageService.GetTableRecord<SourceEntity>(container, source);
                ConnectParameters connector = mapper.Map<ConnectParameters>(entity);
                dbConn.OpenConnection(connector);
                string select = functionAccessDef.Select;
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

        [HttpPost("{source}")]
        public async Task<ActionResult<string>> SaveFunction(string source, RuleFunctions function)
        {
            if (function == null) return BadRequest();
            DbUtilities dbConn = new DbUtilities();
            try
            {
                string tmpConnString = Request.Headers["AzureStorageConnection"];
                tableStorageService.SetConnectionString(tmpConnString);
                fileStorageService.SetConnectionString(tmpConnString);
                string accessJson = await fileStorageService.ReadFile("connectdefinition", "PPDMDataAccess.json");
                List<DataAccessDef> accessDefs = JsonConvert.DeserializeObject<List<DataAccessDef>>(accessJson);
                DataAccessDef functionAccessDef = accessDefs.First(x => x.DataType == "Functions");
                SourceEntity entity = await tableStorageService.GetTableRecord<SourceEntity>(container, source);
                ConnectParameters connector = mapper.Map<ConnectParameters>(entity);
                dbConn.OpenConnection(connector);
                string select = functionAccessDef.Select;
                string query = $" where FunctionName = '{function.FunctionName}'";
                DataTable dt = dbConn.GetDataTable(select, query);
                if (dt.Rows.Count > 0)
                {
                    return BadRequest();
                }
                string jsonInsert = JsonConvert.SerializeObject(function, Formatting.Indented);
                dbConn.InsertDataObject(jsonInsert, "Functions");
            }
            catch (Exception ex)
            {
                return BadRequest();
            }
            dbConn.CloseConnection();
            return Ok($"OK");
        }

        [HttpPut("{source}/{id:int}")]
        public async Task<ActionResult<string>> UpdateFunction(string source, int id, RuleFunctions function)
        {
            if (function == null) return BadRequest();
            DbUtilities dbConn = new DbUtilities();
            try
            {
                string tmpConnString = Request.Headers["AzureStorageConnection"];
                tableStorageService.SetConnectionString(tmpConnString);
                fileStorageService.SetConnectionString(tmpConnString);
                string accessJson = await fileStorageService.ReadFile("connectdefinition", "PPDMDataAccess.json");
                List<DataAccessDef> accessDefs = JsonConvert.DeserializeObject<List<DataAccessDef>>(accessJson);
                DataAccessDef functionAccessDef = accessDefs.First(x => x.DataType == "Functions");
                SourceEntity entity = await tableStorageService.GetTableRecord<SourceEntity>(container, source);
                ConnectParameters connector = mapper.Map<ConnectParameters>(entity);
                dbConn.OpenConnection(connector);
                string select = functionAccessDef.Select;
                string query = $" where Id = {id}";
                DataTable dt = dbConn.GetDataTable(select, query);
                if (dt.Rows.Count == 1)
                {
                    function.Id = id;
                    string jsonInsert = JsonConvert.SerializeObject(function, Formatting.Indented);
                    dbConn.UpdateDataObject(jsonInsert, "Functions");
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
            DbUtilities dbConn = new DbUtilities();
            try
            {
                string tmpConnString = Request.Headers["AzureStorageConnection"];
                tableStorageService.SetConnectionString(tmpConnString);
                fileStorageService.SetConnectionString(tmpConnString);
                string accessJson = await fileStorageService.ReadFile("connectdefinition", "PPDMDataAccess.json");
                List<DataAccessDef> accessDefs = JsonConvert.DeserializeObject<List<DataAccessDef>>(accessJson);
                DataAccessDef functionAccessDef = accessDefs.First(x => x.DataType == "Functions");
                SourceEntity entity = await tableStorageService.GetTableRecord<SourceEntity>(container, source);
                ConnectParameters connector = mapper.Map<ConnectParameters>(entity);
                dbConn.OpenConnection(connector);
                string select = functionAccessDef.Select;
                string query = $" where Id = {id}";
                DataTable dt = dbConn.GetDataTable(select, query);
                if (dt.Rows.Count == 1)
                {
                    string table = "pdo_rule_functions";
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
