using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace DatabaseManager.Services.Reports
{
    public static class SD
    {
        // ─── THREAD-SAFE STORAGE FIELDS ───
        private static readonly AsyncLocal<bool> _sqlite = new();
        private static readonly AsyncLocal<string> _indexApiBase = new();

        // ─── SAFE CONCURRENT PROPERTIES ───
        // These read and write identically to normal properties but stay isolated per request thread
        public static bool Sqlite
        {
            get => _sqlite.Value;
            set => _sqlite.Value = value;
        }

        public static string IndexAPIBase
        {
            get => _indexApiBase.Value;
            set => _indexApiBase.Value = value;
        }

        // ─── STANDARD STATIC CONFIGURATIONS ───
        // These stay as normal properties because their values are set once at startup and never change
        public static string RuleAPIBase { get; set; }
        public static string IndexSqliteAPI { get; set; }
        public static string IndexSqlServerAPI { get; set; }
        public static string IndexKey { get; set; }
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
