using DatabaseManager.Services.DataOpsManagement.Extensions;
using DatabaseManager.Services.DataOpsManagement.Models;
using DatabaseManager.Services.DataOpsManagement.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using System.Net;
using System.Xml;

namespace DatabaseManager.Services.DataOpsManagement
{
    public class DataOpsManagement
    {
        private readonly ILogger<DataOpsManagement> _logger;
        private readonly IFileStorage _fs;
        protected ResponseDto _response;

        public DataOpsManagement(ILogger<DataOpsManagement> logger, IFileStorage fs)
        {
            _logger = logger;
            _fs = fs;
            _response = new ResponseDto();
        }

        [Function("GetDataOpsList")]
        public async Task<HttpResponseData> DataOpsList([HttpTrigger(AuthorizationLevel.Function, "get")] HttpRequestData req)
        {
            _logger.LogInformation("DataOpsList: Starting.");

            try
            {
                string azureStorageAccount = req.GetStorageKey();
                _fs.SetConnectionString(azureStorageAccount);
                List<string> result = await _fs.ListFiles("dataops");
                List<DataOpsPipes> pipes = new List<DataOpsPipes>();
                foreach (string file in result)
                {
                    pipes.Add(new DataOpsPipes { Name = file });
                }
                string jsonResult = JsonConvert.SerializeObject(pipes, Newtonsoft.Json.Formatting.Indented);
                _response.Result = jsonResult;
            }
            catch (Exception ex)
            {
                _response.IsSuccess = false;
                _response.ErrorMessages
                     = new List<string>() { ex.ToString() };
                _logger.LogError($"DataOpsList: Error getting data ops list: {ex}");
            }
            var response = req.CreateResponse(HttpStatusCode.OK);
            await response.WriteAsJsonAsync(_response);
            _logger.LogInformation("DataOpsList: Completed.");
            return response;
        }
    }
}
