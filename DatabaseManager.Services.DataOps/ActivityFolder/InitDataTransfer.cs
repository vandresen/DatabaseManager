using DatabaseManager.Services.DataOps.Models;
using DatabaseManager.Services.DataOps.Services;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;
using System.Text.Json;

namespace DatabaseManager.Services.DataOps.ActivityFolder
{
    public class InitDataTransfer
    {
        private readonly IDataTransferAccess _dataTransfer;

        public InitDataTransfer(IDataTransferAccess dataTransfer)
        {
            _dataTransfer = dataTransfer;
        }

        [Function(nameof(ManageDataOps_InitDataTransfer))]
        public async Task<List<string>> ManageDataOps_InitDataTransfer([ActivityTrigger] DataOpParameters pipe,
            FunctionContext executionContext)
        {
            ILogger log = executionContext.GetLogger("ManageDataOps_InitDataTransfer");
            log.LogInformation($"InitDataTransfer: Starting");
            List<string> files = new List<string>();
            var parms = JsonSerializer.Deserialize<TransferParameters>(pipe.JsonParameters);
            if (parms == null)
            {
                log.LogError("InitDataTransfer: Failed to deserialize TransferParameters.");
                return new List<string>();
            }

            ResponseDto response = await _dataTransfer.GetDataObjects<ResponseDto>(parms.SourceName, pipe.StorageAccount);
            if (response.IsSuccess && response.Result is not null)
            {
                try
                {
                    files = JsonSerializer.Deserialize<List<string>>(response.Result?.ToString() ?? "[]");
                    log.LogInformation($"InitDataTransfer: Number of files are {files.Count}.");
                }
                catch (Exception ex)
                {
                    log.LogError(ex, "InitDataTransfer: Failed to parse response.Result to List<string>.");
                }
            }
            else
            {
                string separator = ";";
                log.LogInformation($"InitDataTransfer: Error {string.Join(separator, response.ErrorMessages)}");
            }

            log.LogInformation($"InitDataTransfer: Complete");
            return files;
        }
     }
}
