using DatabaseManager.Services.DataOps.Models;
using DatabaseManager.Services.DataOps.Services;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;
using System.Text.Json;
//using Newtonsoft.Json;

namespace DatabaseManager.Services.DataOps.ActivityFolder
{
    public class DeleteDataTransfer
    {
        private readonly IDataTransferAccess _dataTransfer;

        public DeleteDataTransfer(IDataTransferAccess dataTransfer)
        {
            _dataTransfer = dataTransfer;
        }

        [Function(nameof(ManageDataOps_DeleteDataTransfer))]
        public async Task<string> ManageDataOps_DeleteDataTransfer([ActivityTrigger] DataOpParameters pipe,
            FunctionContext executionContext)
        {
            ILogger log = executionContext.GetLogger("ManageDataOps_DeleteDataTransfer");
            log.LogInformation($"DeleteDataTransfer: Starting deleting");

            var parms = JsonSerializer.Deserialize<TransferParameters>(pipe.JsonParameters);
            if (parms == null)
            {
                log.LogError("DeleteDataTransfer: Failed to deserialize TransferParameters.");
                return $"DeleteDataTransfer Completed with failure";
            }

            ResponseDto response = await _dataTransfer.DeleteTable<ResponseDto>(parms.TargetName, parms.Table, pipe.StorageAccount);
            if (response.IsSuccess)
            {
                log.LogInformation($"DeleteDataTransfer: Table {parms.Table} deleted");
            }
            else
            {
                string separator = ";";
                log.LogInformation($"DeleteDataTransfer: Error {string.Join(separator, response.ErrorMessages)}");
            }

            log.LogInformation($"DeleteDataTransfer: Complete");
            return $"DeleteDataTransfer Complete";
        }
    }
}
