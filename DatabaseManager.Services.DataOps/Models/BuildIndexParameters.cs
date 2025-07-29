using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DatabaseManager.Services.DataOps.Models
{
    public class BuildIndexParameters
    {
        public string StorageAccount { get; set; }
        public string SourceName { get; set; }
        public string TargetName { get; set; }
        public string Project { get; set; }
        public string Taxonomy { get; set; }
        public string Filter { get; set; }
    }
}
