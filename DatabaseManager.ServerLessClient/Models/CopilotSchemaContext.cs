namespace DatabaseManager.ServerLessClient.Models
{
    public class CopilotSchemaContext
    {
        public List<CopilotDataType> DataTypes { get; set; } = [];
    }

    public class CopilotDataType
    {
        public string Name { get; set; } = "";

        public List<string> Keys { get; set; } = [];

        public Dictionary<string, string> Constants { get; set; } = [];

        public List<CopilotField> Fields { get; set; } = [];
    }

    public class CopilotField
    {
        public string Name { get; set; } = "";

        public string Type { get; set; } = "";
    }
}
