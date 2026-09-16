namespace DatabaseManager.ServerLessClient
{
    public static class SD
    {
        public static bool Sqlite { get; set; }
        public static string GeoBlazorKey { get; set; }
        public enum ApiType
        {
            GET,
            POST,
            PUT,
            DELETE
        }
    }
}
