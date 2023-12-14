using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DatabaseManager.Services.DataQC.Models
{
    public class CurveSpikeParameters
    {
        public int WindowSize { get; set; }
        public int SeveritySize { get; set; }
        public double NullValue { get; set; }
    }
}
