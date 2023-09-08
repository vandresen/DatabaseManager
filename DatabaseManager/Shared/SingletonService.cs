using System;
using System.Collections.Generic;
using System.Text;

namespace DatabaseManager.Shared
{
    public class SingletonServices
    {
        public string BaseUrl { get; set; }
        public string AzureStorage { get; set; }
        public string TargetConnector { get; set; }
        public string DataAccessDefinition { get; set; }
        public string ApiKey { get; set; }
        public bool ServerLess { get; set; }
        public int HttpTimeOut = 500;
        public string Project { get; set; }
    }
}
