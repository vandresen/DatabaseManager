using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Text;

namespace DatabaseManager.Shared
{
    public class ConnectParameters
    {
        [Required]
        public string SourceName { get; set; }
        public string SourceType { get; set; }
        [ValidateDatabase]
        public string Catalog { get; set; }
        [ValidateDatabase]
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
