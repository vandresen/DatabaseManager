using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace DatabaseManager.ServerLessClient.Services
{
    public class SingletonService
    {
        public string BaseUrl { get; set; }
        public string AzureStorage { get; set; }
        public string TargetConnector { get; set; }
        public string DataAccessDefinition { get; set; }
    }
}
