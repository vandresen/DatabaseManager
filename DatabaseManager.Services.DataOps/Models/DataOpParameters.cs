using System.Text.Json.Serialization;

namespace DatabaseManager.Services.DataOps.Models
{
    [JsonConverter(typeof(JsonStringEnumConverter))]
    public enum DatabaseProvider
    {
        SqlServer,
        Sqlite
    }

    public class DataOpsRequest
    {
        public DatabaseProvider DatabaseProvider { get; set; } = DatabaseProvider.SqlServer;
        public List<DataOpParameters> Pipelines { get; set; } = new();
    }

    public class DataOpParameters
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public string Url { get; set; }
        public string StorageAccount { get; set; }
        public string JsonParameters { get; set; }

        // Set by the orchestrator from DataOpsRequest.DatabaseProvider — not set by callers directly
        public DatabaseProvider DatabaseProvider { get; set; } = DatabaseProvider.SqlServer;
    }
}