using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using DatabaseManager.AppFunctions.Entities;
using DatabaseManager.AppFunctions.Helpers;
using DatabaseManager.Shared;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.DurableTask;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.Azure.WebJobs.Host;
using Microsoft.Extensions.Logging;

namespace DatabaseManager.AppFunctions
{
    public class Data
    {
        public string Name { get; set; }
    }

    public static class ManageDataOps
    {
        [FunctionName("ManageDataOps")]
        public static async Task<string> RunOrchestrator(
            [OrchestrationTrigger] IDurableOrchestrationContext context,
            ILogger log)
        {
            //var outputs = new List<string>();

            //// Replace "hello" with the name of your Durable Activity Function.
            //outputs.Add(await context.CallActivityAsync<string>("ManageDataOps_Hello", "Tokyo"));
            //outputs.Add(await context.CallActivityAsync<string>("ManageDataOps_Hello", "Seattle"));
            //outputs.Add(await context.CallActivityAsync<string>("ManageDataOps_Hello", "London"));

            string response = "OK";

            List<DataOpParameters> pipelines = context.GetInput<List<DataOpParameters>>();
            log.LogInformation($"Number of pipelines: {pipelines.Count}.");

            foreach (var pipe in pipelines)
            {
                if (pipe.Name == "CreateIndex")
                {
                    response = await context.CallActivityAsync<string>("ManageDataOps_CreateIndex", pipe);
                }
                else if (pipe.Name == "DataQC")
                {
                    response = await context.CallActivityAsync<string>("ManageDataOps_DataQC", pipe);
                }
                else
                {
                    log.LogInformation($"Artifact {pipe.Name} does not exist");
                }
                
            }
            log.LogInformation($"All pipelines processed");
            return response;
        }

        [FunctionName("ManageDataOps_CreateIndex")]
        public static async Task<string> CreateIndex([ActivityTrigger] DataOpParameters pipe, ILogger log)
        {
            log.LogInformation($"Saying hello to {pipe.Name}.");

            string parameters = pipe.Parameters.ToString();
            log.LogInformation($"Parameters: {parameters}.");
            log.LogInformation($"URL: {pipe.Url}.");

            HttpClient client = new HttpClient();
            try
            {
                client.DefaultRequestHeaders.Remove("AzureStorageConnection");
                client.DefaultRequestHeaders.Add("AzureStorageConnection", pipe.StorageAccount);
                StringContent stringContent = new StringContent(parameters, Encoding.UTF8, "application/json");
                var response = await client.PostAsync(pipe.Url, stringContent);
                if (response.IsSuccessStatusCode)
                {
                    log.LogInformation($"Sucessfully completed");
                }
                else
                {
                    var error = response.Content.ReadAsStringAsync();
                    log.LogInformation($"Failed with error: {error}");
                }
            }
            catch (Exception ex)
            {
                log.LogInformation($"Serious exception {ex}");
            }
            finally
            {
                client.Dispose();
            }

            return $"Hello {pipe.Name}!";
        }

        [FunctionName("ManageDataOps_DataQC")]
        public static string DataQC([ActivityTrigger] DataOpParameters pipe, ILogger log)
        {
            log.LogInformation($"Saying hello to {pipe.Name}.");
            string parameters = pipe.Parameters.ToString();
            log.LogInformation($"Parameters: {parameters}.");
            log.LogInformation($"URL: {pipe.Url}.");

            HttpClient client = new HttpClient();
            try
            {
                client.DefaultRequestHeaders.Remove("AzureStorageConnection");
                client.DefaultRequestHeaders.Add("AzureStorageConnection", pipe.StorageAccount);
                StringContent stringContent = new StringContent(parameters, Encoding.UTF8, "application/json");
                //var response = await client.PostAsync(pipe.Url, stringContent);
                //if (response.IsSuccessStatusCode)
                //{
                //    log.LogInformation($"Sucessfully completed");
                //}
                //else
                //{
                //    var error = response.Content.ReadAsStringAsync();
                //    log.LogInformation($"Failed with error: {error}");
                //}
            }
            catch (Exception ex)
            {
                log.LogInformation($"Serious exception {ex}");
            }
            finally
            {
                client.Dispose();
            }


            return $"Hello {pipe.Name}!";
        }

        [FunctionName("ManageDataOps_HttpStart")]
        public static async Task<HttpResponseMessage> HttpStart(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", "post")] HttpRequestMessage req,
            [DurableClient] IDurableOrchestrationClient starter,
            ExecutionContext context,
            ILogger log)
        {
            List<DataOpParameters> data  = await req.Content.ReadAsAsync<List<DataOpParameters>>();

            string instanceId = await starter.StartNewAsync("ManageDataOps", data);

            log.LogInformation($"Started orchestration with ID = '{instanceId}'.");

            return starter.CreateCheckStatusResponse(req, instanceId);
        }
    }
}