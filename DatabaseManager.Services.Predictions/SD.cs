namespace DatabaseManager.Services.Predictions
{
    public static class SD
    {
        private static readonly AsyncLocal<bool> _sqlite = new();

        public static bool Sqlite
        {
            get => _sqlite.Value;
            set => _sqlite.Value = value;
        }

        // Set once at startup
        public static string IndexSqliteAPI { get; set; }
        public static string IndexSqlServerAPI { get; set; }
        public static string IndexKeySetting { get; set; }

        // Derived per request
        public static string IndexAPIBase => Sqlite ? IndexSqliteAPI : IndexSqlServerAPI;
        public static string IndexKey => Sqlite ? "" : IndexKeySetting;

        public enum ApiType { GET, POST, PUT, DELETE }
    }
}
