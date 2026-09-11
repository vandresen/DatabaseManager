namespace DatabaseManager.Services.Predictions.Models
{
    public class DmIndexDto
    {
        public int Id { get; set; }
        public string DataType { get; set; }
        public string DataKey { get; set; }
        public int NumberOfDataObjects { get; set; }
        public string JsonData { get; set; }
        public string UniqKey { get; set; }
    }
}
