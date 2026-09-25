using DatabaseManager.Services.DataOps.Models;
using DatabaseManager.Services.DataOps.Orchestrators;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.DurableTask.Client;
using Microsoft.Extensions.Logging;
using System.Net;

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

            DataOpsRequest request = await req.ReadFromJsonAsync<DataOpsRequest>();
            if (request is null || request.Pipelines is null || request.Pipelines.Count == 0)
            {
                var badResponse = req.CreateResponse(HttpStatusCode.BadRequest);
                await badResponse.WriteStringAsync("Request body must contain at least one pipeline.");
                return badResponse;
            }

            string instanceId = await client.ScheduleNewOrchestrationInstanceAsync(
                nameof(DataOpsOrchestrator), request);

            logger.LogInformation(
                "Started orchestration with ID = '{instanceId}' using provider '{provider}'.",
                instanceId, request.DatabaseProvider);

            return await client.CreateCheckStatusResponseAsync(req, instanceId);
        }
    }
}