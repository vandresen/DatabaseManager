using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DatabaseManager.Services.DataOps
{
    public static class SD
    {
        public static string RuleAPIBase { get; set; }
        public static string RuleKey { get; set; }
        public static string AzureStorageKey { get; set; }
        public enum ApiType
        {
            GET,
            POST,
            PUT,
            DELETE
        }
    }
}
