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
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json;

namespace DatabaseManager.Server.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class IndexController : ControllerBase
    {
        private string connectionString;
        private readonly string container = "sources";
        private readonly ITableStorageService tableStorageService;
        private readonly IMapper mapper;

        public IndexController(IConfiguration configuration,
            ITableStorageService tableStorageService,
            IMapper mapper
            )
        {
            connectionString = configuration.GetConnectionString("AzureStorageConnection");
            this.tableStorageService = tableStorageService;
            this.mapper = mapper;
        }

        [HttpGet("{source}")]
        public async Task<ActionResult<List<DmsIndex>>> Get(string source)
        {
            
            try
            {
                DbUtilities dbConn = new DbUtilities();
                //List<DmsIndex> index = new List<DmsIndex>();
                string tmpConnString = Request.Headers["AzureStorageConnection"];
                tableStorageService.SetConnectionString(tmpConnString);
                SourceEntity entity = await tableStorageService.GetTableRecord<SourceEntity>(container, source);
                ConnectParameters connector = mapper.Map<ConnectParameters>(entity);
                dbConn.OpenConnection(connector);
                string strProcedure = $"EXEC spGetNumberOfDescendants '/', 1";
                string query = "";
                DataTable qc = dbConn.GetDataTable(strProcedure, query);
                List<DmsIndex>  index = ProcessAllChildren(qc);
                dbConn.CloseConnection();
                return index;
            }
            catch (Exception ex)
            {
                return BadRequest();
            }
        }

        [HttpGet("{source}/{id}")]
        public async Task<ActionResult<string>> GetChildren(string source, int id)
        {
            string tmpConnString = Request.Headers["AzureStorageConnection"];
            tableStorageService.SetConnectionString(tmpConnString);
            SourceEntity entity = await tableStorageService.GetTableRecord<SourceEntity>(container, source);
            ConnectParameters connector = mapper.Map<ConnectParameters>(entity);
            DbUtilities dbConn = new DbUtilities();
            dbConn.OpenConnection(connector);

            DbQueries dbq = new DbQueries();
            string select = dbq["Index"];
            string query = $" where INDEXID = {id}";
            DataTable dt = dbConn.GetDataTable(select, query);
            if (dt.Rows.Count == 0)
                return NotFound();
            string indexNode = dt.Rows[0]["Text_IndexNode"].ToString();
            int indexLevel = Convert.ToInt32(dt.Rows[0]["INDEXLEVEL"]) + 1;

            string result = "[]";
            
            string strProcedure = $"EXEC spGetNumberOfDescendants '{indexNode}', {indexLevel}";
            query = "";
            DataTable idx = dbConn.GetDataTable(strProcedure, query);
            List<DmsIndex> qcIndex = ProcessAllChildren(idx);
 
            dbConn.CloseConnection();
            result = JsonConvert.SerializeObject(qcIndex);
            return result;
        }

        private List<DmsIndex> ProcessAllChildren(DataTable idx)
        {
            List<DmsIndex> qcIndex = new List<DmsIndex>();

            foreach (DataRow idxRow in idx.Rows)
            {
                string dataType = idxRow["DATATYPE"].ToString();
                string indexId = idxRow["INDEXID"].ToString();
                string jsonData = idxRow["JSONDATAOBJECT"].ToString();
                int intIndexId = Convert.ToInt32(indexId);
                int nrOfObjects = Convert.ToInt32(idxRow["NumberOfDataObjects"]);
                qcIndex.Add(new DmsIndex()
                {
                    Id = intIndexId,
                    DataType = dataType,
                    NumberOfDataObjects = nrOfObjects,
                    JsonData = jsonData
                });
            }

            return qcIndex;
        }
    }
}