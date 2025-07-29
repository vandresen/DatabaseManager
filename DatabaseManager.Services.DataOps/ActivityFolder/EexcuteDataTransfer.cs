using DatabaseManager.Services.DataOps.Models;
using DatabaseManager.Services.DataOps.Services;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;
using System.Text.Json;

namespace DatabaseManager.Services.DataOps.ActivityFolder
{
    public class EexcuteDataTransfer
    {
        private readonly IDataTransferAccess _dataTransfer;

        public EexcuteDataTransfer(IDataTransferAccess dataTransfer)
        {
            _dataTransfer = dataTransfer;
        }

        [Function(nameof(ManageDataOps_DataTransfer))]
        public async Task<string> ManageDataOps_DataTransfer([ActivityTrigger] DataOpParameters pipe, FunctionContext executionContext)
        {
            ILogger log = executionContext.GetLogger("ManageDataOps_DataTransfer");
            log.LogInformation($"DataTransfer: Starting transfer");

            var parms = JsonSerializer.Deserialize<TransferParameters>(pipe.JsonParameters);
            if (parms == null)
            {
                log.LogError("DataTransfer: Failed to deserialize TransferParameters.");
                return $"DataTransfer Completed with failure";
            }

            ResponseDto response = await _dataTransfer.Copy<ResponseDto>(parms, pipe.StorageAccount);
            if (response.IsSuccess)
            {
                log.LogInformation($"DataTransfer: Table {parms.Table} copied");
            }
            else
            {
                string separator = ";";
                log.LogInformation($"DataTransfer: Error {string.Join(separator, response.ErrorMessages)}");
            }

            log.LogInformation($"DataTransfer: Complete");
            return $"OK Data transfer complete";
        }
    }
}
