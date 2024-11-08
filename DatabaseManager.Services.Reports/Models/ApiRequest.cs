using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static DatabaseManager.Services.Reports.SD;

namespace DatabaseManager.Services.Reports.Models
{
    public class ApiRequest
    {
        public ApiType ApiType { get; set; } = ApiType.GET;
        public string Url { get; set; }
        public string AzureStorage { get; set; }
        public object Data { get; set; }
    }
}
