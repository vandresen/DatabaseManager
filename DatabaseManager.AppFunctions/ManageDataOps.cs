using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using DatabaseManager.AppFunctions.Entities;
using DatabaseManager.AppFunctions.Helpers;
using DatabaseManager.Common.Helpers;
using DatabaseManager.Shared;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.DurableTask;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.Azure.WebJobs.Host;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

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
            string response = "OK";

            List<DataOpParameters> pipelines = context.GetInput<List<DataOpParameters>>();
            log.LogInformation($"RunOrchestrator: Number of pipelines: {pipelines.Count}.");

            foreach (var pipe in pipelines)
            {
                if (pipe.Name == "CreateIndex")
                {
                    response = await context.CallActivityAsync<string>("ManageDataOps_CreateIndex", pipe);
                }
                else if (pipe.Name == "DataQC")
                {
                    
                    List<QcResult> qcList = await context.CallActivityAsync<List<QcResult>>("ManageDataOps_InitDataQC", pipe);
                    var tasks = new Task<string>[qcList.Count];
                    for (int i = 0; i < qcList.Count; i++)
                    {
                        int qcId = qcList[i].Id;
                        JObject pipeParm = pipe.Parameters;
                        pipeParm["RuleId"] = qcId;
                        pipe.Parameters = pipeParm;
                        string stat = await context.CallActivityAsync<string>("ManageDataOps_DataQC", pipe);
                        //tasks[i] = context.CallActivityAsync<string>("ManageDataOps_DataQC", pipe);
                    }

                    //await Task.WhenAll(tasks);
                }
                else if(pipe.Name == "DataTransfer")
                {
                    log.LogInformation($"Starting data transfer");
                    List<string> files = await context.CallActivityAsync<List<string>>("ManageDataOps_InitDataTransfer", pipe);
                    foreach (string file in files)
                    {
                        JObject pipeParm = pipe.Parameters;
                        pipeParm["Table"] = file;
                        pipe.Parameters = pipeParm;
                        string stat = await context.CallActivityAsync<string>("ManageDataOps_DataTransfer", pipe);
                    }
                }
                else
                {
                    log.LogInformation($"Artifact {pipe.Name} does not exist");
                }
                
            }
            log.LogInformation($"RunOrchestrator: All pipelines processed");
            return response;
        }

        [FunctionName("ManageDataOps_CreateIndex")]
        public static async Task<string> CreateIndex([ActivityTrigger] DataOpParameters pipe, ILogger log)
        {
            log.LogInformation($"CreateIndex: Starting");

            string parameters = pipe.Parameters.ToString();
            
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
                log.LogInformation($"CreateIndex: Serious exception {ex}");
            }
            finally
            {
                client.Dispose();
            }

            log.LogInformation($"CreateIndex: Complete");
            return $"Hello {pipe.Name}!";
        }

        [FunctionName("ManageDataOps_InitDataQC")]
        public static async Task<List<QcResult>> InitDataQC([ActivityTrigger] DataOpParameters pipe, ILogger log)
        {
            log.LogInformation($"InitDataQC: Starting");
            DataQC qc = new DataQC(pipe.StorageAccount);
            DataQCParameters qcParms = pipe.Parameters.ToObject<DataQCParameters>();
            List<QcResult> qcList = await qc.GetQCRules(qcParms);
            await qc.ClearQCFlags(pipe.StorageAccount, qcParms);
            log.LogInformation($"InitDataQC: Complete");
            return qcList;
        }

        [FunctionName("ManageDataOps_InitDataTransfer")]
        public static async Task<List<string>> InitDataTransfer([ActivityTrigger] DataOpParameters pipe, ILogger log)
        {
            log.LogInformation($"InitDataTransfer: Starting");
            
            DataTransfer dt = new DataTransfer(pipe.StorageAccount);
            TransferParameters parms= pipe.Parameters.ToObject<TransferParameters>();
            List<string> files = await dt.GetFiles(parms.SourceName);
            
            log.LogInformation($"InitDataTransfer: Complete");
            return files;
        }

        [FunctionName("ManageDataOps_DataTransfer")]
        public static async Task<string> DataTransfer([ActivityTrigger] DataOpParameters pipe, ILogger log)
        {
            log.LogInformation($"DataTransfer: Starting");
            DataTransfer dt = new DataTransfer(pipe.StorageAccount);
            TransferParameters parms = pipe.Parameters.ToObject<TransferParameters>();
            await dt.CopyFiles(parms);
            log.LogInformation($"DataTransfer: Complete");
            return $"OK";
        }

        [FunctionName("ManageDataOps_DataQC")]
        public static async Task<string> DataQC([ActivityTrigger] DataOpParameters pipe, ILogger log)
        {
            try
            {
                log.LogInformation($"InitDataQC: Starting");
                DataQC qc = new DataQC(pipe.StorageAccount);
                DataQCParameters qcParms = pipe.Parameters.ToObject<DataQCParameters>();
                await qc.ProcessQcRule(qcParms);
            }
            catch (Exception ex)
            {
                log.LogInformation($"InitDataQC:Serious exception {ex}");
            }

            return $"OK";
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