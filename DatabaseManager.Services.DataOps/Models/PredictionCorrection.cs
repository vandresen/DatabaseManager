using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DatabaseManager.Services.DataOps.Models
{
    public class PredictionCorrection
    {
        public int Id { get; set; }

        public string RuleName { get; set; }

        public int NumberOfCorrections { get; set; }

        public string RuleKey { get; set; }
    }
}
