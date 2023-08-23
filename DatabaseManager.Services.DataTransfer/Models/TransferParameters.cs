using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DatabaseManager.Services.DataTransfer.Models
{
    public class TransferParameters
    {
        public string SourceName { get; set; }
        public string SourceType { get; set; }
        public string SourceDataType { get; set; }
        public string TargetName { get; set; }
        public string TransferQuery { get; set; }
        public string WellListFile { get; set; }
        public string QueryType { get; set; }
        public string Table { get; set; }
        public bool Remote { get; set; }
    }
}
