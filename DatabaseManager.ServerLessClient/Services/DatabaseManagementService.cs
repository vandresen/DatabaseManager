using DatabaseManager.ServerLessClient.Helpers;
using DatabaseManager.ServerLessClient.Models;

namespace DatabaseManager.ServerLessClient.Services
{
    public class DatabaseManagementService : BaseService, IDatabaseManagementService
    {
        private readonly ILogger<DatabaseManagementService> _logger;
        private readonly string _databaseManagerAPIBase;
        private readonly string _databaseManagerKey;

        public DatabaseManagementService(IHttpClientFactory clientFactory, ILogger<DatabaseManagementService> logger, IConfiguration configuration) : base(clientFactory)
        {
            _logger = logger;
            _databaseManagerAPIBase = SD.DatabaseManagerAPIBase
                ?? throw new InvalidOperationException("DatabaseManagerAPI is not configured");
            _databaseManagerKey = SD.DatabaseManagerKey
                ?? throw new InvalidOperationException("DatabaseManagerKey is not configured");
        }

        public async Task<T> GetDataAccessDef<T>()
        {
            string url = _databaseManagerAPIBase.BuildFunctionUrl($"/GetDatabaseAccessDefinition", $"", _databaseManagerKey);
            _logger.LogInformation($"Retrieving root index data from url {url}");
            return await this.SendAsync<T>(new ApiRequest()
            {
                ApiType = SD.ApiType.GET,
                Url = url
            });
        }
    }
}
