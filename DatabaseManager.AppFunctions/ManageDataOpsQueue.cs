using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using DatabaseManager.AppFunctions.Helpers;
using DatabaseManager.Shared;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Host;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;

namespace DatabaseManager.AppFunctions
{
    public class ManageDataOpsQueue
    {
        private readonly IHttpService httpService;
        private readonly HttpClient httpClient;

        public ManageDataOpsQueue(IHttpService httpService, HttpClient httpClient)
        {
            this.httpService = httpService;
            this.httpClient = httpClient;
        }

        [FunctionName("DataOpsQue")]
        public async Task Run([QueueTrigger("dataopsqueue", Connection = "AzureStorageConnection")]string myQueueItem, ILogger log)
        {
            log.LogInformation($"C# Queue trigger function processed: {myQueueItem}");

            try
            {
                string fileShare = "dataops";

                DataOpParameters parms = JsonConvert.DeserializeObject<DataOpParameters>(myQueueItem);

                string AzureStorage = parms.StorageAccount;
                httpClient.DefaultRequestHeaders.Remove("AzureStorageConnection");
                httpClient.DefaultRequestHeaders.Add("AzureStorageConnection", AzureStorage);

                log.LogInformation($"URL: {parms.Url}");
                log.LogInformation($"URL: {parms.JsonParameterString}");

                StringContent stringContent = new StringContent(parms.JsonParameterString, Encoding.UTF8, "application/json");
                var response = await httpClient.PostAsync(parms.Url, stringContent);
                if (response.IsSuccessStatusCode)
                {
                    log.LogInformation($"Sucessfully completed");
                }
                else
                {
                    var error = response.Content.ReadAsStringAsync();
                    log.LogInformation($"Failed with error: {error}");
                }

                var builder = new ConfigurationBuilder();
                IConfiguration fileStorageConfiguration = builder.Build();
                IFileStorageService fileStorageService = new AzureFileStorageService(fileStorageConfiguration);
                fileStorageService.SetConnectionString(AzureStorage);

                string fileName = parms.Name + ".txt";
                string dataOpsFile = fileStorageService.ReadFile(fileShare, fileName).GetAwaiter().GetResult();
                log.LogInformation(dataOpsFile);
                List<PipeLine> dataOps = JsonConvert.DeserializeObject<List<PipeLine>>(dataOpsFile);
                int id = parms.Id + 1;
                log.LogInformation($"Id for next aratifact is {id}");
                PipeLine newDataOps = dataOps.Where(s => s.Id == id).FirstOrDefault();
                if (newDataOps == null)
                {
                    log.LogInformation($"End of pipeline");
                }
                else
                {
                    string url = parms.Url.Substring(0, (parms.Url.LastIndexOf('/') + 1)) + newDataOps.ArtifactType;
                    log.LogInformation($"Next artifact URL is {url}");
                    DataOpParameters newParms = new DataOpParameters()
                    {
                        Id = newDataOps.Id,
                        Name = parms.Name,
                        Url = url,
                        StorageAccount = parms.StorageAccount,
                        JsonParameterString = newDataOps.Parameters.ToString()
                    };
                    string json = JsonConvert.SerializeObject(newParms);
                    string message = json.EncodeBase64();
                    string queueName = "dataopsqueue";
                    Utilities.InsertMessage(queueName, message, AzureStorage);
                    log.LogInformation($"Next artifact message sent");
                }
            }
            catch (Exception ex)
            {
                log.LogInformation($"Serious exception {ex}");
            }
        }
    }
}
