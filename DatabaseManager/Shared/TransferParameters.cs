using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Text;

namespace DatabaseManager.Shared
{
    public class TransferParameters
    {
        public string SourceName { get; set; }
        public string SourceType { get; set; }
        public string TargetName { get; set; }
        public string TransferQuery { get; set; }
        public string WellListFile { get; set; }
        public string QueryType { get; set; }
        public string Table { get; set; }
        public bool Remote { get; set; }
    }
}
