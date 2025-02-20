namespace DatabaseManager.Services.Datasources.Models.Dto
{
    public class ConnectParametersDto
    {
        public string SourceName { get; set; }
        public string SourceType { get; set; }
        public string Catalog { get; set; }
        public string DatabaseServer { get; set; }
        public string User { get; set; }
        public string Password { get; set; }
        public string ConnectionString { get; set; }
        public string DataType { get; set; }
        public string FileName { get; set; }
        public int CommandTimeOut { get; set; }
        public string DataAccessDefinition { get; set; }
    }
}
