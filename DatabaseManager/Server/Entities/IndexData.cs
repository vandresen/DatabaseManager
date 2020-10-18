using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace DatabaseManager.Server.Entities
{
    public class IndexData
    {
        public string DataName { get; set; }
        public string DataType { get; set; }
        public string IndexNode { get; set; }
        public string QcLocation { get; set; }
        public double? Latitude { get; set; }
        public double? Longitude { get; set; }
        public string JsonDataObject { get; set; }
        public string DataKey { get; set; }
    }
}
