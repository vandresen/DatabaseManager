using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using System.Linq;
using DatabaseManager.Common.Helpers;
using System.Collections.Generic;
using DatabaseManager.Shared;
using Microsoft.Azure.WebJobs.Extensions.OpenApi.Core.Attributes;
using Microsoft.Azure.WebJobs.Extensions.OpenApi.Core.Enums;
using Microsoft.OpenApi.Models;
using System.Net;

namespace DatabaseManager.AppFunctions
{
    public class GetData
    {
        private readonly ILogger<GetData> log;

        public GetData(ILogger<GetData> log)
        {
            this.log = log;
        }

        [FunctionName("GetDataOpsList")]
        [OpenApiOperation(operationId: "DataOpsList", tags: new[] { "name" })]
        [OpenApiSecurity("function_key", SecuritySchemeType.ApiKey, Name = "code", In = OpenApiSecurityLocationType.Query)]
        [OpenApiResponseWithBody(statusCode: HttpStatusCode.OK, contentType: "text/plain", bodyType: typeof(string), Description = "The OK response")]
        public async Task<IActionResult> DataOpsList(
            [HttpTrigger(AuthorizationLevel.Function, "get", "post", Route = null)] HttpRequest req,
            ExecutionContext context)
        {
            log.LogInformation("GetDataOpsList: Starting.");
            string jsonResult = "OK";

            var headers = req.Headers;
            string storageAccount = headers.FirstOrDefault(x => x.Key == "azurestorageconnection").Value;
            if (string.IsNullOrEmpty(storageAccount))
            {
                log.LogError("GetDataOpsList: error, missing azure storage account");
                return new BadRequestObjectResult("Missing azure storage account in http header");
            }
            DataOpsRepository dops = new DataOpsRepository(storageAccount);
            List<DataOpsPipes> pipes = await dops.GetDataOpsPipes();
            jsonResult = JsonConvert.SerializeObject(pipes, Formatting.Indented);

            log.LogInformation("GetDataOpsList: Completed.");
            return new OkObjectResult(jsonResult);
        }

        [FunctionName("GetDataOpsPipe")]
        [OpenApiOperation(operationId: "DataOpsPipe", tags: new[] { "name" })]
        [OpenApiSecurity("function_key", SecuritySchemeType.ApiKey, Name = "code", In = OpenApiSecurityLocationType.Query)]
        [OpenApiParameter(name: "name", In = ParameterLocation.Query, Required = true, Type = typeof(string), Description = "The **Name** parameter")]
        [OpenApiResponseWithBody(statusCode: HttpStatusCode.OK, contentType: "text/plain", bodyType: typeof(string), Description = "The OK response")]
        public async Task<IActionResult> DataOpsPipe(
            [HttpTrigger(AuthorizationLevel.Function, "get", "post", Route = null)] HttpRequest req,
            ExecutionContext context)
        {
            log.LogInformation("GetDataOpsPipe: Starting.");
            string jsonResult = "OK";

            string pipeName = req.Query["name"];
            //string pipeName = await new StreamReader(req.Body).ReadToEndAsync();
            log.LogInformation($"ExecuteDataOps: {pipeName}");
            if (string.IsNullOrEmpty(pipeName))
            {
                log.LogError("ExecuteDataOps: error, missing pipe name");
                return new BadRequestObjectResult("Missing pipe name in body");
            }

            var headers = req.Headers;
            string storageAccount = headers.FirstOrDefault(x => x.Key == "azurestorageconnection").Value;
            if (string.IsNullOrEmpty(storageAccount))
            {
                log.LogError("GetDataOpsPipe: error, missing azure storage account");
                return new BadRequestObjectResult("Missing azure storage account in http header");
            }
            DataOpsRepository dops = new DataOpsRepository(storageAccount);
            List<PipeLine> dataOps = await dops.GetDataOpsPipe(pipeName);
            jsonResult = JsonConvert.SerializeObject(dataOps, Formatting.Indented);
            log.LogInformation($"GetDataOpsPipe: Json result = {jsonResult}.");

            log.LogInformation("GetDataOpsPipe: Completed.");
            return new OkObjectResult(jsonResult);
        }
    }
}
