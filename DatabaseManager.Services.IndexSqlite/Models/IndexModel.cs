using NetTopologySuite.Geometries;

namespace DatabaseManager.Services.IndexSqlite.Models
{
    public class IndexModel
    {
        public int IndexId { get; set; }
        public int ParentId { get; set; }
        public int IndexLevel { get; set; }
        public string IndexNode { get; set; }
        public string DataName { get; set; }
        public string DataType { get; set; }
        public string DataKey { get; set; }
        public string QC_String { get; set; }
        public string UniqKey { get; set; }
        public string JsonDataObject { get; set; }
        public Double? Latitude { get; set; }
        public Double? Longitude { get; set; }
        public Geometry Locations { get; set; }
    }
}
