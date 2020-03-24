using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Text;

namespace DatabaseManager.Shared
{
    public class TransferParameters
    {
        public string SourceName { get; set; }
        public string SourceDatabase { get; set; }
        public string SourceDatabaseServer { get; set; }
        public string SourceDatabaseUser { get; set; }
        public string SourceDatabasePassword { get; set; }
        public string TargetName { get; set; }
        public string TargetDatabase { get; set; }
        public string TargetDatabaseServer { get; set; }
        public string TargetDatabaseUser { get; set; }
        public string TargetDatabasePassword { get; set; }
        public string TransferQuery { get; set; }
        public string WellListFile { get; set; }
        public string QueryType { get; set; }
        public string Table { get; set; }
    }
}
