namespace DatabaseManager.Services.IndexSqlite.Models
{
    public class LASHeaderMappings
    {
        private readonly Dictionary<string, string> _dictionary;

        public LASHeaderMappings()
        {
            _dictionary = new Dictionary<string, string>();
            _dictionary = new Dictionary<string, string>
                {
                    { "UWI", "UWI" },
                    { "API", "API" },
                    { "LAT", "SURFACE_LATITUDE" },
                    { "LON", "SURFACE_LONGITUDE" },
                    { "COMP", "OPERATOR" },
                    { "WELL", "WELL_NAME" },
                    { "LEAS", "LEASE_NAME" }
                };
        }

        public string this[string key]
        {
            get { return _dictionary[key]; }
        }
    }
}
