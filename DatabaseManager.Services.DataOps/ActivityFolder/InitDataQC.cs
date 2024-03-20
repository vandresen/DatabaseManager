using DatabaseManager.Services.DataOps.Models;
using DatabaseManager.Services.DataOps.Services;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace DatabaseManager.Services.DataOps.ActivityFolder
{
    public class InitDataQC
    {
        private readonly IRuleAccess _ruleAccess;

        public InitDataQC(IRuleAccess ruleAccess)
        {
            _ruleAccess = ruleAccess;
        }
        [Function(nameof(DataOps_InitDataQC))]
        public async Task<List<QcResult>> DataOps_InitDataQC([ActivityTrigger] DataOpParameters pipe,
            FunctionContext executionContext)
        {
            ILogger log = executionContext.GetLogger("DataOps_InitDataQC");
            log.LogInformation($"InitDataQC: Starting");
            DataQCParameters qcParms = JObject.Parse(pipe.JsonParameters).ToObject<DataQCParameters>();
            ResponseDto response = await _ruleAccess.GetRules<ResponseDto>(qcParms.DataConnector);
            if (response.IsSuccess) 
            {
                List<QcResult> qcList = JsonConvert.DeserializeObject<List<QcResult>>(response.Result.ToString());
                log.LogInformation($"InitDataQC: Complete");
                return qcList;
            }
            else
            {
                string error = string.Join(";", response.ErrorMessages);
                log.LogInformation($"DataOps_InitDataQC: Error {error}");
                throw new Exception(error);
            }
        }
    }
}
