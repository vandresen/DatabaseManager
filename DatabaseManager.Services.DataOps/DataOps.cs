using DatabaseManager.Services.DataOps.Models;
using DatabaseManager.Services.DataOps.Orchestrators;
using DatabaseManager.Services.DataOps.Services;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.DurableTask;
using Microsoft.DurableTask.Client;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json.Linq;

namespace DatabaseManager.Services.DataOps
{
    public static class DataOps
    {
        [Function("DataOps_HttpStart")]
        public static async Task<HttpResponseData> HttpStart(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", "post")] HttpRequestData req,
            [DurableClient] DurableTaskClient client,
            FunctionContext executionContext)
        {
            ILogger logger = executionContext.GetLogger("DataOps_HttpStart");
            List<DataOpParameters> pipelines = await req.ReadFromJsonAsync<List<DataOpParameters>>();
            string instanceId = await client.ScheduleNewOrchestrationInstanceAsync(
                nameof(DataOpsOrchestrator), pipelines);
            logger.LogInformation("Started orchestration with ID = '{instanceId}'.", instanceId);
            return await client.CreateCheckStatusResponseAsync(req, instanceId);
        }
    }
}
