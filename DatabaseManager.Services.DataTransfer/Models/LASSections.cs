using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DatabaseManager.Services.DataTransfer.Models
{
    public class LASSections
    {
        public string versionInfo { get; set; }
        public string wellInfo { get; set; }
        public string curveInfo { get; set; }
        public string parameterInfo { get; set; }
        public string dataInfo { get; set; }
    }
}
