namespace DatabaseManager.ServerLessClient.Models
{
    public class PipeLine
    {
        public int Id { get; set; }
        public int Priority { get; set; }
        public string ArtifactType { get; set; }
        public string Parameters { get; set; }
    }
}
