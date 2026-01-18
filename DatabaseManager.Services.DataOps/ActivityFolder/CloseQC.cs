using DatabaseManager.Services.DataOps.Models;
using DatabaseManager.Services.DataOps.Services;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DatabaseManager.Services.DataOps.ActivityFolder
{
    public class CloseQC
    {
        private readonly IDataQc _dataQc;

        public CloseQC(IDataQc dataQc)
        {
            _dataQc = dataQc;
        }

        [Function(nameof(ManageDataOps_CloseDataQC))]
        public async Task<string> ManageDataOps_CloseDataQC([ActivityTrigger] DataQCDataOpsCloseParameters parms, FunctionContext executionContext)
        {
            ILogger log = executionContext.GetLogger("ManageDataOps_CloseDataQC");
            log.LogInformation($"CloseDataQC: Starting");
            DataOpParameters pipe = parms.Parameters;
            DataQCParameters qcParms = JObject.Parse(pipe.JsonParameters).ToObject<DataQCParameters>();
            ResponseDto response = await _dataQc.CloseDataQc<ResponseDto>(qcParms.DataConnector, qcParms.IndexProject, parms.Failures);
            log.LogInformation($"CloseDataQC: Complete");
            return "Data QC Closed";
        }
    }
}
