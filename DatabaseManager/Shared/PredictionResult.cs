using System;
using System.Collections.Generic;
using System.Text;

namespace DatabaseManager.Shared
{
    public class PredictionResult
    {
        public string Status { get; set; }

        public int IndexId { get; set; }

        public string DataType { get; set; }

        public string SaveType { get; set; }

        public string DataObject { get; set; }
    }
}
