using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DatabaseManager.BlazorComponents.Models.RuleParameters
{
    public class PredictionRuleRegistry
    {
        public static readonly Dictionary<string, List<RuleParameterFieldDescriptor>> Descriptors = new()
        {
            ["DeleteDataObject"] = new List<RuleParameterFieldDescriptor>(),
            ["PredictFormationOrder"] = new List<RuleParameterFieldDescriptor>(),
            ["PredictDepthUsingIDW"] = new List<RuleParameterFieldDescriptor>(),
            ["PredictDominantLithology"] = new List<RuleParameterFieldDescriptor>(),
            ["PredictLogDepthAttributes"] = new List<RuleParameterFieldDescriptor>(),
        };
    }
}
