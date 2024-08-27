using DatabaseManager.Services.DataOpsManagement.Extensions;
using DatabaseManager.Services.DataOpsManagement.Models;
using DatabaseManager.Services.DataOpsManagement.Services;
using Microsoft.AspNetCore.Http;
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
        private string fileShare = "dataops";

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
                List<string> result = await _fs.ListFiles(fileShare);
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

        [Function("DeletePipeline")]
        public async Task<HttpResponseData> Delete([HttpTrigger(AuthorizationLevel.Function, "delete")] HttpRequestData req)
        {
            _logger.LogInformation("DeletePipeline: Starting.");

            try
            {
                string azureStorageAccount = req.GetStorageKey();
                _fs.SetConnectionString(azureStorageAccount);

                string name = req.GetQuery("Name", true);
                if (!name.EndsWith(".txt")) name = name + ".txt";
                _logger.LogInformation($"DeletePipeline: Pipe name {name}");
                await _fs.DeleteFile(fileShare, name);
            }
            catch (Exception ex)
            {
                _response.IsSuccess = false;
                _response.ErrorMessages
                     = new List<string>() { ex.ToString() };
                _logger.LogError($"DeletePipeline: Error deleting pipeline: {ex}");
            }
            var response = req.CreateResponse(HttpStatusCode.OK);
            await response.WriteAsJsonAsync(_response);
            _logger.LogInformation("DataOpsList: Completed.");
            return response;
        }
    }
}
