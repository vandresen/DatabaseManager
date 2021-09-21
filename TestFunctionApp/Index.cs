using System;
using System.IO;
using System.Net;
using System.Threading.Tasks;
using DatabaseManager.Common.Helpers;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.Azure.WebJobs.Extensions.OpenApi.Core.Attributes;
using Microsoft.Azure.WebJobs.Extensions.OpenApi.Core.Enums;
using Microsoft.Extensions.Logging;
using Microsoft.OpenApi.Models;
using Newtonsoft.Json;

namespace TestFunctionApp
{
    public class Index
    {
        private readonly ILogger log;

        public Index(ILogger log)
        {
            this.log = log;
        }

        [FunctionName("GetTaxonomies")]
        [OpenApiOperation(operationId: "Run", tags: new[] { "name" })]
        [OpenApiSecurity("function_key", SecuritySchemeType.ApiKey, Name = "code", In = OpenApiSecurityLocationType.Query)]
        [OpenApiParameter(name: "name", In = ParameterLocation.Query, Required = true, Type = typeof(string), Description = "The **Name** parameter")]
        [OpenApiResponseWithBody(statusCode: HttpStatusCode.OK, contentType: "text/plain", bodyType: typeof(string), Description = "The OK response")]
        public async Task<IActionResult> Taxonomies(
            [HttpTrigger(AuthorizationLevel.Function, "get", Route = null)] HttpRequest req)
        {
            log.LogInformation("GetTaxonomies: Starting");
            string responseMessage = "";
            try
            {
                string storageAccount = Common.GetStorageKey(req);
                IndexManagement im = new IndexManagement(storageAccount);
                responseMessage = await im.GetTaxonomies();
            }
            catch (Exception ex)
            {
                //log.LogError($"GetTaxonomies: {ex}");
                return new BadRequestObjectResult($"Error getting taxonomies: {ex}");
            }

            //log.LogInformation("GetTaxonomies: Complete");
            return new OkObjectResult(responseMessage);
        }

    }
}

