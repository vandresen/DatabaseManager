using DatabaseManager.Server.Services;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace DatabaseManager.Server.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class DataOpsController : Controller
    {
        private readonly IFileStorageService fileStorageService;

        public DataOpsController(IFileStorageService fileStorageService)
        {
            this.fileStorageService = fileStorageService;
        }

        [HttpGet()]
        public async Task<ActionResult<List<string>>> Get()
        {
            SetStorageAccount();
            List<string> result = await fileStorageService.ListFiles("dataops");

            return result;
        }

        [HttpPost()]
        public async Task<ActionResult<string>> execute()
        {
            //string baseUrl = @"https://localhost:44386/api/";
            //string storageAccount = "DefaultEndpointsProtocol=https;AccountName=petrodataonlinestorage;AccountKey=5AqpguqLaWYyF0hsDxUX66f8dAyJLnoc6Q4K6Rhngvu9iYn9YubljP4Lc+Hst4hY6MCuuo8ietla5n57b9ScJw==;EndpointSuffix=core.windows.net";

            //var builder = new ConfigurationBuilder();
            //IConfiguration configuration = builder.Build();
            //IFileStorageService fileStorageService = new AzureFileStorageService(configuration);
            //fileStorageService.SetConnectionString(storageAccount);

            //string fileShare = "dataops";
            //string artifactType = "CreateIndex";
            //string pipeLineName = "TestPipeLine";
            //string fileName = pipeLineName + ".txt";

            //fileStorageService.ReadFile(fileShare, fileName);

            //string queueName = "dataopsqueue";

            //string dataOpsFile = fileStorageService.ReadFile(fileShare, fileName).GetAwaiter().GetResult();
            //List<PipeLine> dataOps = JsonConvert.DeserializeObject<List<PipeLine>>(dataOpsFile);
            //PipeLine firstDataOps = dataOps.First();
            //JArray JsonDataOpsArray = JArray.Parse(dataOpsFile);

            //var jToken = JsonDataOpsArray[0];
            //artifactType = (string)jToken["ArtifactType"];
            //int artifactId = (int)jToken["Id"];
            //string parameters = jToken["Parameters"].ToString();
            //DataOpParameters parms = new DataOpParameters()
            //{
            //    Id = firstDataOps.Id,
            //    Name = pipeLineName,
            //    Url = baseUrl + firstDataOps.ArtifactType,
            //    StorageAccount = storageAccount,
            //    JsonParameterString = firstDataOps.Parameters.ToString()
            //};
            //string json = JsonConvert.SerializeObject(parms);
            //string message = json.EncodeBase64();
            //int messageLength = message.Length;
            //InsertMessage(queueName, message);

            return Ok($"OK");
        }

        private void SetStorageAccount()
        {
            string tmpConnString = Request.Headers["AzureStorageConnection"];
            fileStorageService.SetConnectionString(tmpConnString);
        }

        private void InsertMessage(string queueName, string message, string connectionString)
        {
            //QueueClient queueClient = new QueueClient(connectionString, queueName);

            //queueClient.CreateIfNotExists();

            //if (queueClient.Exists())
            //{
            //    queueClient.SendMessage(message);
            //}
        }
    }
}
