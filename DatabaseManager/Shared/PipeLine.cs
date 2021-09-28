using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Text;

namespace DatabaseManager.Shared
{
    public class PipeLine
    {
        public int Id { get; set; }
        public int Priority { get; set; }
        public string ArtifactType { get; set; }
        public string Parameters { get; set; }
    }
}
