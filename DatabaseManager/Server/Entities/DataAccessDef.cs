using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace DatabaseManager.Server.Entities
{
    public class DataAccessDef
    {
        public string DataType { get; set; }
        public string Select { get; set; }
        public string Keys { get; set; }
    }
}
