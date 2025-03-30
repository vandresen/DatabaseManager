using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DatabaseManager.Services.Reports.Models
{
    public class IndexFileData
    {
        public string DataName { get; set; }

        public string NameAttribute { get; set; }

        public string ParentKey { get; set; }

        public string LatitudeAttribute { get; set; }

        public string LongitudeAttribute { get; set; }

        public JToken Arrays { get; set; }

        public Boolean UseParentLocation { get; set; }

        public DataTable DataTable { get; set; }
    }
}
