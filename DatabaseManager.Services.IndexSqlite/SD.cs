namespace DatabaseManager.Services.IndexSqlite
{
    public class SD
    {
        public static string DataSourceAPIBase { get; set; }
        public static string DataSourceKey { get; set; }
        public static string DataConfigurationAPIBase { get; set; }
        public static string DataConfigurationKey { get; set; }
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
