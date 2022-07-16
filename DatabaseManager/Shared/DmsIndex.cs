using System;
using System.Collections.Generic;
using System.Text;

namespace DatabaseManager.Shared
{
    public class DmsIndex
    {
        public int Id { get; set; }
        public string DataType { get; set; }
        public string DataKey { get; set; }
        public int NumberOfDataObjects { get; set; }
        public string JsonData { get; set; }
        public string UniqKey { get; set; }
    }
}
