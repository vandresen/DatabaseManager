using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static DatabaseManager.BlazorComponents.SD;

namespace DatabaseManager.BlazorComponents.Models
{
    public class ApiRequest
    {
        public ApiType ApiType { get; set; } = ApiType.GET;
        public string Url { get; set; }
        public string AzureStorage { get; set; }
        public object Data { get; set; }
    }
}
