namespace DatabaseManager.ServerLessClient
{
    public static class SD
    {
        //public static string DataOpsManageAPIBase { get; set; }
        //public static string DataOpsManageKey { get; set; }
        //public static string DataOpsAPIBase { get; set; }
        //public static string DataOpsKey { get; set; }
        //public static string DataTransferAPIBase { get; set; }
        //public static string DataTransferKey { get; set; }
        public static bool Sqlite { get; set; }
        public static string EsriKey { get; set; }
        public static string? OpenAIKey { get; set; }
        public enum ApiType
        {
            GET,
            POST,
            PUT,
            DELETE
        }
    }
}
