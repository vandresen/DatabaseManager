using System;
using System.Collections.Generic;
using System.Data;
using System.Text;

namespace DatabaseManager.Shared
{
    public class IndexFileData
    {
        public string DataName { get; set; }

        public string NameAttribute { get; set; }

        public string Select { get; set; }

        public string ParentKey { get; set; }

        public string LatitudeAttribute { get; set; }

        public string LongitudeAttribute { get; set; }

        public string Keys { get; set; }

        public Boolean UseParentLocation { get; set; }

        public DataTable DataTable { get; set; }
    }
}
