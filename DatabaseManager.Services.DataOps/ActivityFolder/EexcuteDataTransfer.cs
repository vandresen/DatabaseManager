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
            log.LogInformation("DataTransfer: Starting transfer");

            var parms = JsonSerializer.Deserialize<TransferParameters>(pipe.JsonParameters);
            if (parms == null)
            {
                log.LogError("DataTransfer: Failed to deserialize TransferParameters.");
                throw new InvalidOperationException("DataTransfer: Failed to deserialize TransferParameters.");
            }

            ResponseDto response = await _dataTransfer.Copy<ResponseDto>(parms, pipe.StorageAccount);

            if (response == null || !response.IsSuccess)
            {
                string errors = string.Join(";", response?.ErrorMessages ?? Enumerable.Empty<string>());
                log.LogError("DataTransfer: Error for table {table}: {errors}", parms.Table, errors);
                throw new InvalidOperationException($"DataTransfer failed for table '{parms.Table}': {errors}");
            }

            log.LogInformation("DataTransfer: Table {table} copied", parms.Table);
            return "OK Data transfer complete";
        }
    }
}
