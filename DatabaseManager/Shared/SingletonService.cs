using System;
using System.Collections.Generic;
using System.Diagnostics;
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
        public string IndexKey { get; set; }
        public string IndexAPIBase { get; set; }
        public string DataOpsManageAPIBase { get; set; }
        public string DataOpsManageKey { get; set; }
        public string DataOpsAPIBase { get; set; }
        public string DataOpsKey { get; set; }
        public string DataConfigurationAPIBase { get; set; }
        public string DataConfigurationKey { get; set; }
        public string DataModelAPIBase { get; set; }
        public string DataModelKey { get; set; }
        public string DataRuleAPIBase { get; set; }
        public string DataRuleKey { get; set; }
        public string DataTransferAPIBase { get; set; }
        public string DataTransferKey { get; set; }
    }
}
