using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using DatabaseManager.AppFunctions.Entities;
using DatabaseManager.AppFunctions.Helpers;
using DatabaseManager.Common.Helpers;
using DatabaseManager.Common.Services;
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
                        JObject pipeParm = JObject.Parse(pipe.JsonParameters);
                        pipeParm["RuleId"] = qcId;
                        pipe.JsonParameters = pipeParm.ToString();
                        string stat = await context.CallActivityAsync<string>("ManageDataOps_DataQC", pipe);
                        //tasks[i] = context.CallActivityAsync<string>("ManageDataOps_DataQC", pipe);
                    }

                    //await Task.WhenAll(tasks);
                }
                else if(pipe.Name == "DataTransfer")
                {
                    log.LogInformation($"Starting data transfer");
                    List<string> files = await context.CallActivityAsync<List<string>>("ManageDataOps_InitDataTransfer", pipe);

                    TransferParameters parms = JObject.Parse(pipe.JsonParameters).ToObject<TransferParameters>();
                    if (parms.SourceType == "DataBase")
                    {
                        foreach (string file in files)
                        {
                            JObject pipeParm = JObject.Parse(pipe.JsonParameters);
                            pipeParm["Table"] = file;
                            pipe.JsonParameters = pipeParm.ToString();
                            string stat = await context.CallActivityAsync<string>("ManageDataOps_DeleteDataTransfer", pipe);
                        }
                    }

                    foreach (string file in files)
                    {
                        JObject pipeParm = JObject.Parse(pipe.JsonParameters);
                        pipeParm["Table"] = file;
                        pipe.JsonParameters = pipeParm.ToString();
                        string stat = await context.CallActivityAsync<string>("ManageDataOps_DataTransfer", pipe);
                    }
                }
                else if (pipe.Name == "Predictions")
                {
                    log.LogInformation($"Starting Predictions");
                    List<PredictionCorrection> predictionList = await context.CallActivityAsync<List<PredictionCorrection>>("ManageDataOps_InitPredictions", pipe);
                    var tasks = new Task<string>[predictionList.Count];
                    JObject pipeParm = JObject.Parse(pipe.JsonParameters);
                    for (int i = 0; i < predictionList.Count; i++)
                    {
                        int id = predictionList[i].Id;
                        pipeParm["PredictionId"] = id;
                        pipe.JsonParameters = pipeParm.ToString();
                        string stat = await context.CallActivityAsync<string>("ManageDataOps_Prediction", pipe);
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

        [FunctionName("ManageDataOps_InitPredictions")]
        public static async Task<List<PredictionCorrection>> InitPredictions([ActivityTrigger] DataOpParameters pipe, ILogger log)
        {
            log.LogInformation($"InitPredictions: Starting");
            List<PredictionCorrection> predictionList = new List<PredictionCorrection>();
            Predictions predictions = new Predictions(pipe.StorageAccount);
            PredictionParameters parms = JObject.Parse(pipe.JsonParameters).ToObject<PredictionParameters>();
            predictionList = await predictions.GetPredictions(parms.DataConnector);
            log.LogInformation($"Number of predictions are {predictionList.Count}");
            log.LogInformation($"InitPredictions: Complete");
            return predictionList;
        }

        [FunctionName("ManageDataOps_CreateIndex")]
        public static async Task<string> CreateIndex([ActivityTrigger] DataOpParameters pipe, ILogger log)
        {
            log.LogInformation($"CreateIndex: Starting");
            CreateIndexParameters parms = JObject.Parse(pipe.JsonParameters).ToObject<CreateIndexParameters>();
            try
            {
                Sources sr = new Sources(pipe.StorageAccount);
                ConnectParameters target = await sr.GetSourceParameters(parms.TargetName);
                ConnectParameters source = await sr.GetSourceParameters(parms.SourceName);

                Indexer index = new Indexer(pipe.StorageAccount);
                int parentNodes = await index.Initialize(target, source, parms.Taxonomy);

                List<ParentIndexNodes> nodes = await index.IndexParent(parentNodes);

                for (int j = 0; j < nodes.Count; j++)
                {
                    ParentIndexNodes node = nodes[j];
                    for (int i = 0; i < node.NodeCount; i++)
                    {
                        await index.IndexChildren(j, i, node.ParentNodeId);
                    }
                }

                index.CloseIndex();

            }
            catch (Exception ex)
            {
                log.LogInformation($"CreateIndex: Serious exception {ex}");
            }

            log.LogInformation($"CreateIndex: Complete");
            return $"Hello {pipe.Name}!";
        }

        [FunctionName("ManageDataOps_InitDataQC")]
        public static async Task<List<QcResult>> InitDataQC([ActivityTrigger] DataOpParameters pipe, ILogger log)
        {
            log.LogInformation($"InitDataQC: Starting");
            DataQC qc = new DataQC(pipe.StorageAccount);
            DataQCParameters qcParms = JObject.Parse(pipe.JsonParameters).ToObject<DataQCParameters>();
            List<QcResult> qcList = await qc.GetQCRules(qcParms);
            await qc.ClearQCFlags(qcParms.DataConnector);
            log.LogInformation($"InitDataQC: Complete");
            return qcList;
        }

        [FunctionName("ManageDataOps_InitDataTransfer")]
        public static async Task<List<string>> InitDataTransfer([ActivityTrigger] DataOpParameters pipe, ILogger log)
        {
            log.LogInformation($"InitDataTransfer: Starting");
            
            DataTransfer dt = new DataTransfer(pipe.StorageAccount);
            TransferParameters parms= JObject.Parse(pipe.JsonParameters).ToObject<TransferParameters>();
            List<string> files = await dt.GetFiles(parms.SourceName);
            
            log.LogInformation($"InitDataTransfer: Complete");
            return files;
        }

        [FunctionName("ManageDataOps_DeleteDataTransfer")]
        public static async Task<string> DeleteDataTransfer([ActivityTrigger] DataOpParameters pipe, ILogger log)
        {
            log.LogInformation($"DeleteDataTransfer: Starting deleting");
            DataTransfer dt = new DataTransfer(pipe.StorageAccount);
            TransferParameters parms = JObject.Parse(pipe.JsonParameters).ToObject<TransferParameters>();
            await dt.DeleteTable(parms.TargetName, parms.Table);
            log.LogInformation($"DeleteDataTransfer: Complete deleting {parms.Table}");
            return $"DeleteDataTransfer Complete";
        }

        [FunctionName("ManageDataOps_DataTransfer")]
        public static async Task<string> DataTransfer([ActivityTrigger] DataOpParameters pipe, ILogger log)
        {
            log.LogInformation($"DataTransfer: Starting");
            DataTransfer dt = new DataTransfer(pipe.StorageAccount);
            TransferParameters parms = JObject.Parse(pipe.JsonParameters).ToObject<TransferParameters>();
            await dt.CopyFiles(parms);
            log.LogInformation($"DataTransfer: Complete copying {parms.Table}");
            return $"OK";
        }

        [FunctionName("ManageDataOps_DataQC")]
        public static async Task<string> DataQC([ActivityTrigger] DataOpParameters pipe, ILogger log)
        {
            try
            {
                log.LogInformation($"InitDataQC: Starting");
                DataQC qc = new DataQC(pipe.StorageAccount);
                DataQCParameters qcParms = JObject.Parse(pipe.JsonParameters).ToObject<DataQCParameters>();
                await qc.ProcessQcRule(qcParms);
            }
            catch (Exception ex)
            {
                log.LogInformation($"InitDataQC:Serious exception {ex}");
            }

            return $"OK";
        }

        [FunctionName("ManageDataOps_Prediction")]
        public static async Task<string> DataPrediction([ActivityTrigger] DataOpParameters pipe, ILogger log)
        {
            try
            {
                log.LogInformation($"Prediction: Starting prediction");
                Predictions predictions = new Predictions(pipe.StorageAccount);
                PredictionParameters parms = JObject.Parse(pipe.JsonParameters).ToObject<PredictionParameters>();
                log.LogInformation($"Prediction: Processing prediction id {parms.PredictionId}");
                await predictions.ExecutePrediction(parms);
                log.LogInformation($"Prediction: Complete");
            }
            catch (Exception ex)
            {
                log.LogInformation($"Prediction:Serious exception {ex}");
            }

            return $"Prediction Rule {pipe.Id}";
        }

        [FunctionName("ManageDataOps_HttpStart")]
        public static async Task<HttpResponseMessage> HttpStart(
            [HttpTrigger(AuthorizationLevel.Function, "get", "post")] HttpRequestMessage req,
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