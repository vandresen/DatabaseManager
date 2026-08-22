using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DatabaseManager.BlazorComponents.Models.RuleParameters
{
    // Represents one segment of the Uniqueness rule's key expression, e.g.
    //   WELL_ID
    //   *NORMALIZE(UWI,None)
    //   *NORMALIZE14(API_NUMBER)
    // Segments are joined with ';' to build RuleModel.RuleParameters,
    // matching what DataQcCore.CalculateKey expects.
    public class UniquenessKeyPart
    {
        public string Attribute { get; set; }
        // "None", "*NORMALIZE" or "*NORMALIZE14"
        public string Function { get; set; } = "None";
        // Only used when Function == "*NORMALIZE"
        public string NormalizeParameter { get; set; }
        public string Value { get; set; }
    }
}
