namespace DatabaseManager.ServerLessClient
{
    public static class SD
    {
        public static string DataSourceAPIBase { get; set; }
        public static string DataSourceKey { get; set; }
        public static string IndexAPIBase { get; set; }
        public static string IndexKey { get; set; }
        public static string DataOpsManageAPIBase { get; set; }
        public static string DataOpsManageKey { get; set; }
        public static string DataOpsAPIBase { get; set; }
        public static string DataOpsKey { get; set; }
        public static string DataConfigurationAPIBase { get; set; }
        public static string DataConfigurationKey { get; set; }
        public static string DataModelAPIBase { get; set; }
        public static string DataModelKey { get; set; }
        public static string DataRuleAPIBase { get; set; }
        public static string DataRuleKey { get; set; }
        public static string DataTransferAPIBase { get; set; }
        public static string DataTransferKey { get; set; }
        public static bool Sqlite { get; set; }
        public static string EsriKey { get; set; }
        public enum ApiType
        {
            GET,
            POST,
            PUT,
            DELETE
        }
    }
}
