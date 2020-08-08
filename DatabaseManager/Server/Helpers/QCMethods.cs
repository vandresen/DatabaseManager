using DatabaseManager.Shared;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace DatabaseManager.Server.Helpers
{
    public class QCMethods
    {
        public QCMethods()
        {

        }

        public string ProcessMethod(QcRuleSetup qcSetup)
        {
            RuleModel rule = JsonConvert.DeserializeObject<RuleModel>(qcSetup.RuleObject);
            string returnStatus = "Passed";

            switch (rule.RuleType)
            {
                case "Completeness":
                    returnStatus = ProcessCompletenessMethod(qcSetup);
                    break;
                default:
                    break;
            }

            
            return returnStatus;
        }

        public string ProcessCompletenessMethod(QcRuleSetup qcSetup)
        {
            string returnStatus = "Passed";
            RuleModel rule = JsonConvert.DeserializeObject<RuleModel>(qcSetup.RuleObject);
            JObject dataObject = JObject.Parse(qcSetup.DataObject);
            JToken value = dataObject.GetValue(rule.DataAttribute);
            if (value == null)
            {
                //log.Warning($"Attribute is Null");
            }
            else
            {
                string strValue = value.ToString();
                if (string.IsNullOrWhiteSpace(strValue))
                {
                    returnStatus = "Failed";
                }
                else
                {
                    double number;
                    bool canConvert = double.TryParse(strValue, out number);
                    if (canConvert)
                    {
                        if (number == -99999) returnStatus = "Failed";
                    }
                }
            }
            return returnStatus;
        }
    }
}
