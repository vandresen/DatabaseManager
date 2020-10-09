using DatabaseManager.Shared;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Threading.Tasks;

namespace DatabaseManager.Server.Helpers
{
    static class PredictionMethods
    {
        public static PredictionResult DeleteDataObject(QcRuleSetup qcSetup, DbUtilities dbConn)
        {
            PredictionResult result = new PredictionResult();

            result.SaveType = "Delete";
            result.Status = "Passed";
            result.IndexId = qcSetup.IndexId;

            return result;
        }

        public static PredictionResult PredictDepthUsingIDW(QcRuleSetup qcSetup, DbUtilities dbConn)
        {
            double? depth = null;
            PredictionResult result = new PredictionResult
            {
                Status = "Failed"
            };
            DataTable nb = RuleMethodUtilities.GetNeighbors(dbConn, qcSetup);
            if (nb != null)
            {
                depth = RuleMethodUtilities.CalculateDepthUsingIdw(nb, qcSetup);
            }

            if (depth != null)
            {
                RuleModel rule = JsonConvert.DeserializeObject<RuleModel>(qcSetup.RuleObject);
                JObject dataObject = JObject.Parse(qcSetup.DataObject);
                dataObject[rule.DataAttribute] = depth;
                string remark = dataObject["REMARK"] + $";{rule.DataAttribute} has been predicted by QCEngine;";
                dataObject["REMARK"] = remark;
                result.DataObject = dataObject.ToString();
                result.DataType = rule.DataType;
                result.SaveType = "Update";
                result.IndexId = qcSetup.IndexId;
                result.Status = "Passed";
            }

            return result;
        }
    }
}
