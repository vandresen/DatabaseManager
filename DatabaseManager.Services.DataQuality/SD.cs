namespace DatabaseManager.Services.DataQuality
{
    public static class SD
    {
        private static readonly AsyncLocal<bool> _sqlite = new();
        private static readonly AsyncLocal<string> _indexApiBase = new();

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

        public static string IndexSqliteAPI { get; set; }
        public static string IndexSqlServerAPI { get; set; }
        public static string IndexKey { get; set; }
        public enum ApiType
        {
            GET,
            POST,
            PUT,
            DELETE
        }
    }
}
