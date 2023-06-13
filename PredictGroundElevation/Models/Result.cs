using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PredictGroundElevation.Models
{
    public class Result
    {
        public string Status { get; set; }

        public int IndexId { get; set; }

        public string DataType { get; set; }

        public string SaveType { get; set; }

        public string DataObject { get; set; }
    }
}
