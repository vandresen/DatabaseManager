using DatabaseManager.Services.DataOps.Models;
using DatabaseManager.Services.DataOps.Services;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DatabaseManager.Services.DataOps.ActivityFolder
{
    public class DataQC
    {
        private readonly IDataQc _dataQc;

        public DataQC(IDataQc dataQc)
        {
            _dataQc = dataQc;
        }

        [Function(nameof(ManageDataOps_DataQC))]
        public async Task<List<int>> ManageDataOps_DataQC([ActivityTrigger] DataOpParameters pipe, FunctionContext executionContext)
        {
            ILogger log = executionContext.GetLogger("ManageDataOps_DataQC");
            List<int> result = new List<int>();
            try
            {
                
                log.LogInformation($"DataQC: Starting");
                DataQCParameters qcParms = JObject.Parse(pipe.JsonParameters).ToObject<DataQCParameters>();
                qcParms.AzureStorageKey = pipe.StorageAccount;
                JObject pipeParm = JObject.Parse(pipe.JsonParameters);
                qcParms.RuleId = (int)pipeParm["RuleId"];
                ResponseDto response = await _dataQc.ExecuteDataQc<ResponseDto>(qcParms);
                if (response.IsSuccess)
                {
                    result = JsonConvert.DeserializeObject<List<int>>(response.Result.ToString());
                    log.LogInformation($"DataQC: completed rule id {qcParms.RuleId} with result = {result.Count} failures");
                }
                else
                {
                    string error = string.Join(";", response.ErrorMessages);
                    log.LogInformation($"DataQC: Error {error}");
                    throw new Exception(error);
                }
            }
            catch (Exception ex)
            {
                log.LogInformation($"InitDataQC:Serious exception {ex}");
            }

            return result;
        }
    }
}
