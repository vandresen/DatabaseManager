using DatabaseManager.Services.DataOps.Models;
using DatabaseManager.Services.DataOps.Services;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;
using System.Text.Json;

namespace DatabaseManager.Services.DataOps.ActivityFolder
{
    public class CreateIndex
    {
        private readonly IIndexAccess _indexAccess;

        public CreateIndex(IIndexAccess indexAccess)
        {
            _indexAccess = indexAccess;
        }

        [Function(nameof(ManageDataOps_CreateIndex))]
        public async Task<string> ManageDataOps_CreateIndex([ActivityTrigger] DataOpParameters pipe, FunctionContext executionContext)
        {
            ILogger log = executionContext.GetLogger("ManageDataOps_CreateIndex");
            log.LogInformation($"CreateIndex: Starting indexing");

            try
            {
                var parms = JsonSerializer.Deserialize<BuildIndexParameters>(pipe.JsonParameters);
                if (parms == null)
                {
                    log.LogError("DataTransfer: Failed to deserialize TransferParameters.");
                    return $"DataTransfer Completed with failure";
                }

                parms.StorageAccount = pipe.StorageAccount;
                ResponseDto response = await _indexAccess.BuildIndex<ResponseDto>(parms);
                if (response != null && response.IsSuccess)
                {
                    log.LogInformation($"CreateIndex: Index created");
                }
                else
                {
                    string separator = ";";
                    string errors = response?.ErrorMessages != null
                        ? string.Join(separator, response.ErrorMessages)
                        : "Unknown error";

                    log.LogInformation($"CreateIndex: Error {errors}");
                }
            }
            catch (Exception ex)
            {
                log.LogInformation($"CreateIndex: Serious exception {ex}");
            }

            log.LogInformation($"CreateIndex: Complete");
            return $"OK index complete";
        }
    }
}
