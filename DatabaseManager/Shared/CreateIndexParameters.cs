using System;
using System.Collections.Generic;
using System.Text;

namespace DatabaseManager.Shared
{
    public class CreateIndexParameters
    {
        public string StorageAccount { get; set; }
        public string SourceName { get; set; }
        public string TargetName { get; set; }
        public string Taxonomy { get; set; }
        public string Filter { get; set; }
        public string Project { get; set; }
    }
}
