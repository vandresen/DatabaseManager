using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using DatabaseManager.Server.Entities;
using DatabaseManager.Server.Helpers;
using DatabaseManager.Server.Services;
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
    public class FileController : ControllerBase
    {
        private readonly string connectionString;
        private readonly string container = "sources";
        private readonly IFileStorageService fileStorageService;
        private readonly IWebHostEnvironment _env;

        public FileController(IConfiguration configuration,
            IFileStorageService fileStorageService,
            IWebHostEnvironment env)
        {
            connectionString = configuration.GetConnectionString("AzureStorageConnection");
            this.fileStorageService = fileStorageService;
            _env = env;
        }

        [HttpGet("{datatype}")]
        public async Task<ActionResult<List<string>>> Get(string datatype)
        {
            try
            {
                string tmpConnString = Request.Headers["AzureStorageConnection"];
                fileStorageService.SetConnectionString(tmpConnString);
                List<string> files = await fileStorageService.ListFiles(datatype);
                return files;
            }
            catch (Exception)
            {
                return NotFound();
            }
        }

        [HttpPost]
        public async Task<ActionResult<string>> SaveData(FileParameters fileParams)
        {
            if (fileParams == null) return BadRequest();
            try
            {
                string tmpConnString = Request.Headers["AzureStorageConnection"];
                fileStorageService.SetConnectionString(tmpConnString);
                ConnectParameters connector = Common.GetConnectParameters(connectionString, container, fileParams.DataConnector);

                string accessJson = await fileStorageService.ReadFile("connectdefinition", "PPDMDataAccess.json");
                List<DataAccessDef> accessDefs = JsonConvert.DeserializeObject<List<DataAccessDef>>(accessJson);

                string referenceJson = await fileStorageService.ReadFile("connectdefinition", "PPDMReferenceTables.json");
                List<ReferenceTable> referenceDefs = JsonConvert.DeserializeObject<List<ReferenceTable>>(referenceJson);

                string fileText = await fileStorageService.ReadFile(fileParams.FileShare, fileParams.FileName);
                var lines = fileText.CountLines();
                if (lines == 0)
                {
                    Exception error = new Exception($"Empty data from {fileParams.FileName}");
                    throw error;
                }
                else if (lines > 20000)
                {
                    Exception error = new Exception($"{fileParams.FileName} are too large");
                    throw error;
                }
                else
                {
                    if (fileParams.FileShare == "logs")
                    {
                        LASLoader ls = new LASLoader(_env, accessDefs, referenceDefs);
                        ls.LoadLASFile(connector, fileText);
                    }
                    else
                    {
                        string csvJson = await fileStorageService.ReadFile("connectdefinition", "CSVDataAccess.json");
                        List<CSVAccessDef> csvDef = JsonConvert.DeserializeObject<List<CSVAccessDef>>(csvJson);
                        string connectionString = connector.ConnectionString;
                        string[] fileNameArray = fileParams.FileName.Split('.');
                        string dataType = fileNameArray[0].Remove(fileNameArray[0].Length - 1, 1);
                        CSVLoader cl = new CSVLoader(_env, accessDefs, referenceDefs, csvDef);
                        cl.LoadCSVFile(connectionString, fileText, dataType);
                    }
                }
            }
            catch (Exception ex)
            {
                return BadRequest(ex.ToString());
            }

            return Ok($"OK");
        }
    }
}