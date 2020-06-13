using System;
using System.Collections.Generic;
using System.Text;

namespace DatabaseManager.Shared
{
    public class PredictionCorrection
    {
        public int Id { get; set; }

        public string RuleName { get; set; }

        public int NumberOfCorrections { get; set; }

        public string RuleKey { get; set; }
    }
}
