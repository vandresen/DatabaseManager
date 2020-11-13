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
        public string Database { get; set; }
        [ValidateDatabase]
        public string DatabaseServer { get; set; }
        public string DatabaseUser { get; set; }
        public string DatabasePassword { get; set; }
        public string ConnectionString { get; set; }
        public string DataType { get; set; }
        public string FileShare { get; set; }
        public string FileName { get; set; }
        public string DataAccessDefinition { get; set; }
    }
}
