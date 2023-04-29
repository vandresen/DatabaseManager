using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DatabaseManager.Services.DataTransfer.Models
{
    public class LASLine
    {
        public string Mnem { get; set; }
        public string Unit { get; set; }
        public string Data { get; set; }
        public string Description { get; set; }
    }
}
