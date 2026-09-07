using Blazored.LocalStorage;

namespace DatabaseManager.ServerLessClient.Models
{
    public class BlazorSingletonService
    {
        private const string StorageKey = "AppSettings";


        public string BaseUrl { get; set; }
        public string TargetConnector { get; set; }
        public string DataAccessDefinition { get; set; }
        public string ApiKey { get; set; }
        public bool ServerLess { get; set; }
        public int HttpTimeOut { get; set; } = 500;
        public string Project { get; set; }

        public string AzureStorage { get; set; }
        public string GeoBlazorRegistrationKey { get; set; }

        public string DataSourceAPI { get; set; }
        public string DataSourceKey { get; set; }

        public string IndexAPI { get; set; }
        public string IndexKey { get; set; }

        public string DataConfigurationAPI { get; set; }
        public string DataConfigurationKey { get; set; }

        public string DataRuleAPI { get; set; }

        public string DataOpsManageAPI { get; set; }
        public string DataOpsManageKey { get; set; }

        public string DataOpsAPI { get; set; }
        public string DataOpsKey { get; set; }

        public string ReportApiBase { get; set; }
        public string ReportKey { get; set; }

        public string DatabaseManagerAPI { get; set; }
        public string DatabaseManagerKey { get; set; }

        public string OpenAIApiKey { get; set; }
        public string EsriKey { get; set; }

        public bool IsLoaded { get; private set; }

        public async Task LoadFromLocalStorageAsync(ILocalStorageService localStorage, bool forceReload = false)
        {
            if (IsLoaded && !forceReload) return;

            try
            {
                var stored = await localStorage.GetItemAsync<BlazorSingletonService>(StorageKey);
                if (stored != null)
                {
                    CopyFrom(stored);
                }
            }
            catch
            {
                // First run / corrupt entry - keep defaults rather than throwing.
            }

            IsLoaded = true;
        }

        public async Task SaveToLocalStorageAsync(ILocalStorageService localStorage)
        {
            await localStorage.SetItemAsync(StorageKey, this);
        }

        public void ApplyImport(SettingsImportModel import)
        {
            if (import == null) return;

            AzureStorage = import.AzureStorage;
            GeoBlazorRegistrationKey = import.GeoBlazorRegistrationKey;
            DataSourceAPI = import.DataSourceAPI;
            DataSourceKey = import.DataSourceKey;
            IndexAPI = import.IndexAPI;
            IndexKey = import.IndexKey;
            DataConfigurationAPI = import.DataConfigurationAPI;
            DataConfigurationKey = import.DataConfigurationKey;
            DataRuleAPI = import.DataRuleAPI;
            DataOpsManageAPI = import.DataOpsManageAPI;
            DataOpsManageKey = import.DataOpsManageKey;
            DataOpsAPI = import.DataOpsAPI;
            DataOpsKey = import.DataOpsKey;
            ReportApiBase = import.ReportApiBase;
            ReportKey = import.ReportKey;
            DatabaseManagerAPI = import.DatabaseManagerAPI;
            DatabaseManagerKey = import.DatabaseManagerKey;
            OpenAIApiKey = import.OpenAIApiKey;
            EsriKey = import.EsriKey;
        }

        public BlazorSingletonService Clone() => (BlazorSingletonService)MemberwiseClone();

        public void CopyFrom(BlazorSingletonService other)
        {
            if (other == null) return;

            BaseUrl = other.BaseUrl;
            TargetConnector = other.TargetConnector;
            DataAccessDefinition = other.DataAccessDefinition;
            ApiKey = other.ApiKey;
            ServerLess = other.ServerLess;
            HttpTimeOut = other.HttpTimeOut;

            AzureStorage = other.AzureStorage;
            GeoBlazorRegistrationKey = other.GeoBlazorRegistrationKey;
            DataSourceAPI = other.DataSourceAPI;
            DataSourceKey = other.DataSourceKey;
            IndexAPI = other.IndexAPI;
            IndexKey = other.IndexKey;
            DataConfigurationAPI = other.DataConfigurationAPI;
            DataConfigurationKey = other.DataConfigurationKey;
            DataRuleAPI = other.DataRuleAPI;
            DataOpsManageAPI = other.DataOpsManageAPI;
            DataOpsManageKey = other.DataOpsManageKey;
            DataOpsAPI = other.DataOpsAPI;
            DataOpsKey = other.DataOpsKey;
            ReportApiBase = other.ReportApiBase;
            ReportKey = other.ReportKey;
            DatabaseManagerAPI = other.DatabaseManagerAPI;
            DatabaseManagerKey = other.DatabaseManagerKey;
            OpenAIApiKey = other.OpenAIApiKey;
            EsriKey = other.EsriKey;
        }

        public List<string> GetMissingRequiredSettings()
        {
            var missing = new List<string>();

            if (string.IsNullOrEmpty(AzureStorage)) missing.Add("Azure Storage");
            if (string.IsNullOrEmpty(IndexAPI) || IndexKey == null) missing.Add("Index API");
            if (string.IsNullOrEmpty(DataSourceAPI) || string.IsNullOrEmpty(DataSourceKey)) missing.Add("Data Source API");
            if (string.IsNullOrEmpty(DataOpsAPI) || string.IsNullOrEmpty(DataOpsKey)) missing.Add("Data Ops API");
            if (string.IsNullOrEmpty(DataOpsManageAPI) || string.IsNullOrEmpty(DataOpsManageKey)) missing.Add("Data Ops Manager API");
            if (string.IsNullOrEmpty(ReportApiBase) || string.IsNullOrEmpty(ReportKey)) missing.Add("Report API");
            if (string.IsNullOrEmpty(DataConfigurationAPI) || string.IsNullOrEmpty(DataConfigurationKey)) missing.Add("Data Configuration API");
            if (string.IsNullOrEmpty(DataRuleAPI)) missing.Add("Data Rule API");

            return missing;
        }
    }
}
