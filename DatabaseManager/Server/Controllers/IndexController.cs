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
            DbUtilities dbConn = new DbUtilities();
            List<DmsIndex> index = new List<DmsIndex>();
            try
            {
                string tmpConnString = Request.Headers["AzureStorageConnection"];
                tableStorageService.SetConnectionString(tmpConnString);
                SourceEntity entity = await tableStorageService.GetTableRecord<SourceEntity>(container, source);
                ConnectParameters connector = mapper.Map<ConnectParameters>(entity);
                dbConn.OpenConnection(connector);
                DbQueries dbq = new DbQueries();
                string select = dbq["Index"];
                string query = " where INDEXLEVEL = 1";
                DataTable dt = dbConn.GetDataTable(select, query);
                foreach (DataRow qcRow in dt.Rows)
                {
                    string dataType = qcRow["Dataname"].ToString();
                    string indexNode = qcRow["Text_IndexNode"].ToString();
                    int indexId = Convert.ToInt32(qcRow["INDEXID"]);
                    string strProcedure = $"EXEC spGetDescendants '{indexNode}'";
                    query = "";
                    DataTable qc = dbConn.GetDataTable(strProcedure, query);
                    int nrOfObjects = qc.Rows.Count - 1;
                    index.Add(new DmsIndex()
                    {
                        Id = indexId,
                        DataType = dataType,
                        NumberOfDataObjects = nrOfObjects
                    });
                }
            }
            catch (Exception)
            {
                return BadRequest();
            }

            dbConn.CloseConnection();
            return index;
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
            string dataType = dt.Rows[0]["DATATYPE"].ToString();
            string indexNode = dt.Rows[0]["Text_IndexNode"].ToString();
            int indexLevel = Convert.ToInt32(dt.Rows[0]["INDEXLEVEL"]) + 1;
            query = $" WHERE IndexNode.IsDescendantOf('{indexNode}') = 1 and INDEXLEVEL = {indexLevel}";
            DataTable idx = dbConn.GetDataTable(select, query);

            string result = "[]";
            if (idx.Rows.Count > 0)
            {
                result = ProcessAllChildren(dbConn, idx);
            }

            dbConn.CloseConnection();
            return result;
        }

        private string ProcessAllChildren(DbUtilities dbConn, DataTable idx)
        {
            List<DmsIndex> qcIndex = new List<DmsIndex>();

            foreach (DataRow idxRow in idx.Rows)
            {
                string dataType = idxRow["DATATYPE"].ToString();
                string indexId = idxRow["INDEXID"].ToString();
                string jsonData = idxRow["JSONDATAOBJECT"].ToString();
                int intIndexId = Convert.ToInt32(indexId);
                string indexNode = idxRow["Text_IndexNode"].ToString();
                string strProcedure = $"EXEC spGetDescendants '{indexNode}'";
                string query = "";
                DataTable qc = dbConn.GetDataTable(strProcedure, query);
                int nrOfObjects = qc.Rows.Count - 1;
                qcIndex.Add(new DmsIndex()
                {
                    Id = intIndexId,
                    DataType = dataType,
                    NumberOfDataObjects = nrOfObjects,
                    JsonData = jsonData
                });
            }
            string jsonString = JsonConvert.SerializeObject(qcIndex);

            return jsonString;
        }
    }
}