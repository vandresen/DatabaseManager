namespace DatabaseManager.ServerLessClient.Models
{
    public class SettingsImportModel
    {
        public string AzureStorage { get; set; }
        public string GeoBlazorRegistrationKey { get; set; }

        public string DataSourceAPI { get; set; }
        public string DataSourceKey { get; set; }

        public string IndexAPI { get; set; }
        public string IndexKey { get; set; }

        public string DataConfigurationAPI { get; set; }
        public string DataConfigurationKey { get; set; }

        public string DataModelAPI { get; set; }
        public string DataModelKey { get; set; }

        public string DataRuleAPI { get; set; }
        public string DataRuleKey { get; set; }

        public string DataOpsManageAPI { get; set; }
        public string DataOpsManageKey { get; set; }

        public string DataOpsAPI { get; set; }
        public string DataOpsKey { get; set; }

        public string DataTransferAPI { get; set; }
        public string DataTransferKey { get; set; }

        public string ReportApiBase { get; set; }
        public string ReportKey { get; set; }

        public string DatabaseManagerAPI { get; set; }
        public string DatabaseManagerKey { get; set; }

        public string EsriKey { get; set; }
        public string OpenAIApiKey { get; set; }

    }
}
