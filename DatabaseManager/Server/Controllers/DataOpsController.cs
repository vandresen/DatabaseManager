using Azure.Storage.Queues;
using DatabaseManager.Server.Helpers;
using DatabaseManager.Server.Services;
using DatabaseManager.Shared;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

namespace DatabaseManager.Server.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class DataOpsController : Controller
    {
        private static HttpClient client = new HttpClient();
        private readonly ILogger<DataOpsController> logger;
        private readonly IFileStorageService fileStorageService;
        private string fileShare = "dataops";
        private string connectionString;
        private string dataOpsQueue;
        private string serverUrl;

        public DataOpsController(IConfiguration configuration,
            ILogger<DataOpsController> logger,
            IFileStorageService fileStorageService)
        {
            connectionString = configuration.GetConnectionString("AzureStorageConnection");
            dataOpsQueue = configuration["DataOpsQueue"];
            serverUrl = configuration["BaseUrl"];
            this.logger = logger;
            this.fileStorageService = fileStorageService;
        }

        [HttpGet()]
        public async Task<ActionResult<List<DataOpsPipes>>> Get()
        {
            SetStorageAccount();
            List<DataOpsPipes> pipes = new List<DataOpsPipes>();
            List<string> result = await fileStorageService.ListFiles("dataops");
            foreach (string file in result)
            {
                pipes.Add(new DataOpsPipes { Name = file });
            }
            return pipes;
        }

        [HttpGet("{name}")]
        public async Task<ActionResult<string>> GetPipeline(string name)
        {
            SetStorageAccount();
            //List<PipeLine> dataOps = new List<PipeLine>();
            string dataOpsFile = await fileStorageService.ReadFile(fileShare, name);
            //dataOps = JsonConvert.DeserializeObject<List<PipeLine>>(dataOpsFile);
            return dataOpsFile;
        }

        [HttpPost()]
        public async Task<ActionResult<string>> execute(List<DataOpParameters> parms)
        {
            SetStorageAccount();
            //string baseUrl = $"{Request.Scheme}://{Request.Host.Value.ToString()}{Request.PathBase.Value.ToString()}/api/";
            string storageAccount = connectionString;
            foreach (var item in parms)
            {
                item.StorageAccount = storageAccount;
            }
            

            //string queueName = dataOpsQueue;
            //string fileShare = "dataops";

            //string dataOpsFile = await fileStorageService.ReadFile(fileShare, name);
            //List<PipeLine> dataOps = JsonConvert.DeserializeObject<List<PipeLine>>(dataOpsFile);

            //List<DataOpParameters> parms = new List<DataOpParameters>();
            //foreach (var pipe in dataOps)
            //{
            //    parms.Add(new DataOpParameters()
            //    {
            //        Id = pipe.Id,
            //        Name = pipe.ArtifactType,
            //        Url = baseUrl + pipe.ArtifactType,
            //        StorageAccount = storageAccount,
            //        Parameters = pipe.Parameters
            //    });
            //}

            try
            {
                var jsonString = JsonConvert.SerializeObject(parms);
                var content = new StringContent(jsonString, Encoding.UTF8, "application/json");
                string url = serverUrl + "/ManageDataOps_HttpStart";
                HttpResponseMessage response = client.PostAsync(url, content).Result;
                using (HttpContent respContent = response.Content)
                {
                    string result = respContent.ReadAsStringAsync().Result;
                }
            }
            catch (Exception ex)
            {
                logger.LogWarning($"DataOpsController: Problems with URL, {ex}");
            }
            finally
            {
                client.Dispose();
            }

            return Ok($"OK");
        }

        [HttpPost("CreatePipeline/{name}")]
        public async Task<ActionResult<string>> CreatePipeline(string name)
        {
            if (name == null) return BadRequest("Missing name");
            try
            {
                string fileName = name;
                if (!name.EndsWith(".txt")) fileName = name + ".txt";
                SetStorageAccount();
                await fileStorageService.SaveFile(fileShare, fileName, "");
            }
            catch (Exception ex)
            {
                return BadRequest();
            }
            return Ok($"OK");
        }

        [HttpDelete("{name}")]
        public async Task<ActionResult> Delete(string name)
        {
            if (name == null) return BadRequest("Missing name");
            try
            {
                string fileName = name;
                if (!name.EndsWith(".txt")) fileName = name + ".txt";
                SetStorageAccount();
                await fileStorageService.DeleteFile(fileShare, fileName);
            }
            catch (Exception)
            {
                return BadRequest();
            }

            return NoContent();
        }

        private void SetStorageAccount()
        {
            string tmpConnString = Request.Headers["AzureStorageConnection"];
            if (string.IsNullOrEmpty(connectionString)) connectionString = tmpConnString;
            fileStorageService.SetConnectionString(tmpConnString);
        }

        private void InsertMessage(string queueName, string message, string connectionString)
        {
            QueueClient queueClient = new QueueClient(connectionString, queueName);

            queueClient.CreateIfNotExists();

            if (queueClient.Exists())
            {
                queueClient.SendMessage(message);
            }
        }
    }
}
