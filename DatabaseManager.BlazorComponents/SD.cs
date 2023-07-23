using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DatabaseManager.BlazorComponents
{
    public static class SD
    {
        public static string DataSourceAPIBase { get; set; }
        public static string DataSourceKey { get; set; }
        public static string IndexAPIBase { get; set; }
        public static string IndexKey { get; set; }
        public static string DataConfigurationAPIBase { get; set; }
        public static string DataConfigurationKey { get; set; }
        public static string DataModelAPIBase { get; set; }
        public static string DataModelKey { get; set; }
        public static string DataRuleAPIBase { get; set; }
        public static string DataRuleKey { get; set; }
        public static bool Sqlite { get; set; }
        public enum ApiType
        {
            GET,
            POST,
            PUT,
            DELETE
        }
    }
}
