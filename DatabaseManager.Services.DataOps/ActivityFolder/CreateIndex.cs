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
            log.LogInformation("CreateIndex: Starting indexing");

            try
            {
                var parms = JsonSerializer.Deserialize<BuildIndexParameters>(pipe.JsonParameters);
                if (parms == null)
                {
                    log.LogError("CreateIndex: Failed to deserialize BuildIndexParameters.");
                    throw new InvalidOperationException("CreateIndex failed: deserialization error");
                }

                parms.StorageAccount = pipe.StorageAccount;
                ResponseDto response = await _indexAccess.BuildIndex<ResponseDto>(parms);

                if (response is { IsSuccess: true })
                {
                    log.LogInformation("CreateIndex: Index created successfully");
                    return "OK index complete";
                }

                string errors = response?.ErrorMessages != null
                    ? string.Join(";", response.ErrorMessages)
                    : "Unknown error";

                log.LogError("CreateIndex: Error - {Errors}", errors);
                throw new InvalidOperationException($"CreateIndex failed: {errors}");
            }
            catch (TaskCanceledException ex)
            {
                log.LogError(ex, "CreateIndex: Index build timed out after 6 minutes");
                throw new TimeoutException($"CreateIndex timed out: the index is too large to build in the allowed time. " +
                       $"Please reduce the size of your index by applying a more restrictive filter " +
                       $"such as a smaller lat/lon window or a single county.", ex);
            }
        }
     }
}
