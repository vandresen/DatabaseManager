using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DatabaseManager.Services.DatabaseManagement.Models
{
    public class ForeignKeyInfo
    {
        public string FkName { get; set; }
        public string SchemaName { get; set; }
        public string Table { get; set; }
        public string ReferencedTable { get; set; }
        public string ReferencedColumn { get; set; }
    }
}
