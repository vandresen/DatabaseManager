namespace DatabaseManager.ServerLessClient
{
    public static class SD
    {
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
