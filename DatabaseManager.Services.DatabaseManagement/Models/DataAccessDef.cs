using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DatabaseManager.Services.DatabaseManagement.Models
{
    public class DataAccessDef
    {
        public string DataType { get; set; }
        public string Select { get; set; }
        public string Keys { get; set; }
        public string Constants { get; set; }
    }
}
