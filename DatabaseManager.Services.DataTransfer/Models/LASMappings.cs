using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DatabaseManager.Services.DataTransfer.Models
{
    public class LASMappings
    {
        public List<WellMapping> WellMappings { get; set; }
        public List<AlernativeKey> AlernativeKeys { get; set; }
    }
}
