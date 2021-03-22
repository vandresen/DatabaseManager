using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Text;

namespace DatabaseManager.Shared
{
    public class PipeLine
    {
        public int Id { get; set; }
        public string ArtifactType { get; set; }
        public JObject Parameters { get; set; }
    }
}
