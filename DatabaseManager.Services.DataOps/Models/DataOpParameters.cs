using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DatabaseManager.Services.DataOps.Models
{
    public class DataOpParameters
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public string Url { get; set; }
        public string StorageAccount { get; set; }
        public string JsonParameters { get; set; }
    }
}
