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
                string baseText = $"Starting pipe number {pipe.Id} with name {pipe.Name}";
                context.SetCustomStatus(baseText);
                if (pipe.Name == "CreateIndex")
                {
                    try
                    {
                        response = await context.CallActivityAsync<string>("ManageDataOps_CreateIndex", pipe);
                    }
                    catch (TaskFailedException ex)
                    {
                        string errorMessage = $"Pipeline '{pipe.Name}' (Id: {pipe.Id}) failed: {ex.Message}";
                        log.LogError(ex, errorMessage);
                        context.SetCustomStatus(errorMessage);
                        return errorMessage;
                    }
                    
                }
                else if (pipe.Name == "DataQC")
                {
                    log.LogInformation($"RunOrchestrator: Starting Data QC");
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
                    string statusText = baseText + " Getting prediction list";
                    context.SetCustomStatus(statusText);
                    List<QcResult> predictionList = await context.CallActivityAsync<List<QcResult>>("DataOps_InitPredictions", pipe);
                    if (predictionList is null || predictionList.Count == 0)
                    {
                        log.LogInformation("No predictions to process, skipping.");
                        context.SetCustomStatus("No predictions to process");
                    }
                    else
                    {
                        PredictionParameters pipeParm = JObject.Parse(pipe.JsonParameters).ToObject<PredictionParameters>()
                            ?? throw new InvalidOperationException("Failed to deserialize PredictionParameters.");

                        List<string> predictionResults = new List<string>();
                        List<string> predictionFailures = new List<string>();

                        for (int i = 0; i < predictionList.Count; i++)
                        {
                            int id = predictionList[i].Id;
                            statusText = baseText + $" Processing prediction {i+1}. RuleId: {id}";
                            context.SetCustomStatus(statusText);
                            log.LogInformation($"Processing prediction {i + 1} of {predictionList.Count}, RuleId: {id}");

                            try
                            {
                                pipeParm.PredictionId = id;
                                pipe.JsonParameters = JsonConvert.SerializeObject(pipeParm);
                                string stat = await context.CallActivityAsync<string>("DataOps_Prediction", pipe);
                                log.LogInformation(stat);
                                predictionResults.Add(stat);
                            }
                            catch (TaskFailedException ex)
                            {
                                string errorMessage = $"Prediction {id} (pipe Id: {pipe.Id}) failed: {ex.Message}";
                                log.LogError(ex, errorMessage);
                                predictionFailures.Add(errorMessage);
                                // don't return, don't rethrow — just move to the next prediction
                            }
                        }

                        string summary = $"Predictions complete: {predictionResults.Count} succeeded, {predictionFailures.Count} failed";
                        log.LogInformation(summary);
                        context.SetCustomStatus(predictionFailures.Count == 0
                            ? summary
                            : $"{summary} | Failures: {string.Join(" | ", predictionFailures)}");
                    }
                }
                context.SetCustomStatus($"Completed pipe number {pipe.Id} with name {pipe.Name}");
            }
            log.LogInformation($"RunOrchestrator: All pipelines processed");
            return response;
        }
    }
}
