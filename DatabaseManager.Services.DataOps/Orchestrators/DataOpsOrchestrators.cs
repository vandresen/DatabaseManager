using DatabaseManager.Services.DataOps.Models;
using Microsoft.Azure.Functions.Worker;
using Microsoft.DurableTask;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace DatabaseManager.Services.DataOps.Orchestrators
{
    public static class DataOpsOrchestrator
    {
        [Function(nameof(DataOpsOrchestrator))]
        public static async Task<string> RunOrchestrator(
            [OrchestrationTrigger] TaskOrchestrationContext context)
        {
            ILogger log = context.CreateReplaySafeLogger(nameof(DataOps));
            string response = "OK";
            List<DataOpParameters> pipelines = context.GetInput<List<DataOpParameters>>();
            log.LogInformation($"RunOrchestrator: Number of pipelines: {pipelines.Count}.");
            foreach (var pipe in pipelines)
            {
                context.SetCustomStatus($"Starting pipe number {pipe.Id} with name {pipe.Name}");
                if (pipe.Name == "CreateIndex")
                {
                    //response = await context.CallActivityAsync<string>("ManageDataOps_CreateIndex", pipe);
                }
                else if (pipe.Name == "DataQC")
                {

                    List<QcResult> qcList = await context.CallActivityAsync<List<QcResult>>("DataOps_InitDataQC", pipe);
                    var tasks = new Task<List<int>>[qcList.Count];
                    for (int i = 0; i < qcList.Count; i++)
                        //for (int i = 0; i < 1; i++)
                        {
                        int qcId = qcList[i].Id;
                        JObject pipeParm = JObject.Parse(pipe.JsonParameters);
                        pipeParm["RuleId"] = qcId;
                        pipe.JsonParameters = pipeParm.ToString();
                        tasks[i] = context.CallActivityAsync<List<int>>("ManageDataOps_DataQC", pipe);
                    }

                    await Task.WhenAll(tasks);
                    List<RuleFailures> failures = new List<RuleFailures>();
                    for (int i = 0; i < qcList.Count; i++)
                    {
                        failures.Add(new RuleFailures { RuleId = qcList[i].Id, Failures = tasks[i].Result });
                    }
                    DataQCDataOpsCloseParameters parms = new DataQCDataOpsCloseParameters()
                    {
                        Parameters = pipe,
                        Failures = failures
                    };
                    log.LogInformation($"RunOrchestrator: Ready to close data QC");
                    string stat = await context.CallActivityAsync<string>("ManageDataOps_CloseDataQC", parms);
                }
                else if (pipe.Name == "DataTransfer")
                {
                    log.LogInformation($"Starting data transfer");
                    List<string> files = await context.CallActivityAsync<List<string>>("ManageDataOps_InitDataTransfer", pipe);

                    TransferParameters parms = JsonConvert.DeserializeObject<TransferParameters>(pipe.JsonParameters);
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
                    //List<PredictionCorrection> predictionList = await context.CallActivityAsync<List<PredictionCorrection>>("ManageDataOps_InitPredictions", pipe);
                    //var tasks = new Task<string>[predictionList.Count];
                    //JObject pipeParm = JObject.Parse(pipe.JsonParameters);
                    //for (int i = 0; i < predictionList.Count; i++)
                    //{
                    //    int id = predictionList[i].Id;
                    //    pipeParm["PredictionId"] = id;
                    //    pipe.JsonParameters = pipeParm.ToString();
                    //    string stat = await context.CallActivityAsync<string>("ManageDataOps_Prediction", pipe);
                    //}
                }
                else
                {
                    log.LogInformation($"Artifact {pipe.Name} does not exist");
                }
                context.SetCustomStatus($"Completed pipe number {pipe.Id} with name {pipe.Name}");
            }
            log.LogInformation($"RunOrchestrator: All pipelines processed");
            return response;
        }
    }
}
