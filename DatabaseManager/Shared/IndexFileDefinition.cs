using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DatabaseManager.Shared
{
    public class IndexFileDefinition
    {
        public int Id { get; set; }

        public string DataName { get; set; }

        public string NameAttribute { get; set; }

        public string ParentKey { get; set; }

        public string LatitudeAttribute { get; set; }

        public string LongitudeAttribute { get; set; }

        public string Select { get; set; }

        public Boolean UseParentLocation { get; set; }

        public List<IndexFileDefinition> DataObjects { get; set; }
    }
}
