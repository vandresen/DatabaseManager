using System;
using System.Collections.Generic;
using System.Text;

namespace DatabaseManager.Shared
{
    public class CreateIndexParameters
    {
        public string DataConnector { get; set; }
        public string Taxonomy { get; set; }
        public string ConnectDefinition { get; set; }
        public string IndexName { get; set; }
        public string ParentNodeName { get; set; }
        public int ParentNodeId { get; set; }
        public int ParentNodeNumber { get; set; }
        public int ParentNumber { get; set; }
    }
}
