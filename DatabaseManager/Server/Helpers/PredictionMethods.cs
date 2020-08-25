using DatabaseManager.Shared;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Threading.Tasks;

namespace DatabaseManager.Server.Helpers
{
    public class PredictionMethods
    {
        public PredictionMethods()
        {
                
        }

        public PredictionResult ProcessMethod(QcRuleSetup qcSetup, DataTable dt)
        {
            PredictionResult result = new PredictionResult();
            RuleModel rule = JsonConvert.DeserializeObject<RuleModel>(qcSetup.RuleObject);

            switch (rule.RuleFunction)
            {
                case "DeleteDataObject":
                    result = ProcessDeleteDataObjectMethod(qcSetup);
                    break;
                default:
                    break;
            }
            return result;
        }

        private PredictionResult ProcessDeleteDataObjectMethod(QcRuleSetup qcSetup)
        {
            PredictionResult result = new PredictionResult();

            result.SaveType = "Delete";
            result.Status = "Passed";
            result.IndexId = qcSetup.IndexId;

            return result;
        }
    }
}
