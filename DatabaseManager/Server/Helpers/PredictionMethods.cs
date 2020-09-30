using DatabaseManager.Shared;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Threading.Tasks;

namespace DatabaseManager.Server.Helpers
{
    static class PredictionMethods
    {
        public static PredictionResult DeleteDataObject(QcRuleSetup qcSetup)
        {
            PredictionResult result = new PredictionResult();

            result.SaveType = "Delete";
            result.Status = "Passed";
            result.IndexId = qcSetup.IndexId;

            return result;
        }
    }
}
