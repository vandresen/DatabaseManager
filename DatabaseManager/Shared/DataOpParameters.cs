using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Text;

namespace DatabaseManager.Shared
{
    public class DataOpParameters
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public string Url { get; set; }
        public string StorageAccount { get; set; }
        public JObject Parameters { get; set; }
    }
}
