using System;
using System.Collections.Generic;
using System.Text;

namespace DatabaseManager.Shared
{
    public class SingletonService
    {
        public string BaseUrl { get; set; }
        public string AzureStorage { get; set; }
        public string TargetConnector { get; set; }
        public string DataAccessDefinition { get; set; }
        public string ApiKey { get; set; }
    }
}
