using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DatabaseManager.Services.Index.Models
{
    public class ReferenceTable
    {
        public string DataType { get; set; }
        public string Table { get; set; }
        public string KeyAttribute { get; set; }
        public string ValueAttribute { get; set; }
        public string ReferenceAttribute { get; set; }
        public string FixedKey { get; set; }
        public bool Insert { get; set; }
    }
}
