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
        public async Task<ActionResult<List<string>>> Get()
        {
            SetStorageAccount();
            List<string> result = await fileStorageService.ListFiles("dataops");
            return result;
        }

        [HttpPost("{name}")]
        public async Task<ActionResult<string>> execute(string name)
        {
            SetStorageAccount();
            string baseUrl = $"{Request.Scheme}://{Request.Host.Value.ToString()}{Request.PathBase.Value.ToString()}/api/";
            string storageAccount = connectionString;

            string queueName = dataOpsQueue;
            string fileShare = "dataops";

            string dataOpsFile = await fileStorageService.ReadFile(fileShare, name);
            List<PipeLine> dataOps = JsonConvert.DeserializeObject<List<PipeLine>>(dataOpsFile);

            List<DataOpParameters> parms = new List<DataOpParameters>();
            foreach (var pipe in dataOps)
            {
                parms.Add(new DataOpParameters()
                {
                    Id = pipe.Id,
                    Name = pipe.ArtifactType,
                    Url = baseUrl + pipe.ArtifactType,
                    StorageAccount = storageAccount,
                    Parameters = pipe.Parameters
                });
            }

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

            //PipeLine firstDataOps = dataOps.First();
            //JArray JsonDataOpsArray = JArray.Parse(dataOpsFile);

            //var jToken = JsonDataOpsArray[0];
            //string artifactType = (string)jToken["ArtifactType"];
            //int artifactId = (int)jToken["Id"];
            //string parameters = jToken["Parameters"].ToString();
            //DataOpParameters parms = new DataOpParameters()
            //{
            //    Id = firstDataOps.Id,
            //    Name = name,
            //    Url = baseUrl + firstDataOps.ArtifactType,
            //    StorageAccount = storageAccount,
            //    JsonParameterString = firstDataOps.Parameters.ToString()
            //};
            //string json = JsonConvert.SerializeObject(parms);
            //string message = json.EncodeBase64();
            //int messageLength = message.Length;
            //InsertMessage(queueName, message, storageAccount);

            return Ok($"OK");
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
