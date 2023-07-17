using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DatabaseManager.Services.Index.Models
{
    public class ParentIndexNodes
    {
        public string Name { get; set; }
        public int NodeCount { get; set; }
        public string ParentNodeId { get; set; }
        public int ParentId { get; set; }
    }
}
