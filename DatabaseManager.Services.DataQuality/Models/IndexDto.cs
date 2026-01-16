using System;
using System.Collections.Generic;
using System.Text;

namespace DatabaseManager.Services.DataQuality.Models
{
    public class IndexDto
    {
        public int IndexId { get; set; }
        public string TextIndexNode { get; set; }
        public int IndexLevel { get; set; }
        public string DataName { get; set; }
        public string DataType { get; set; }
        public string DataKey { get; set; }
        public string QC_String { get; set; }
        public string UniqKey { get; set; }
        public string JsonDataObject { get; set; }
        public Double? Latitude { get; set; }
        public Double? Longitude { get; set; }
    }
}
