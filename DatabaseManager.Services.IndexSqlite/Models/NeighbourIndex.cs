namespace DatabaseManager.Services.IndexSqlite.Models
{
    public class NeighbourIndex
    {
        public int IndexId { get; set; }
        public string DataName { get; set; }
        public double Latitude { get; set; }
        public double Longitude { get; set; }
        public string DataKey { get; set; }
        public double Distance { get; set; }
        public string JsonDataObject { get; set; }
        public double Depth { get; set; }

    }
}
