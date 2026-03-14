using DatabaseManager.Services.Predictions.Extensions;
using DatabaseManager.Services.Predictions.Models;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DatabaseManager.Services.Predictions.Services
{
    public class DatabaseManagementService : BaseService, IDatabaseManagementService
    {
        private readonly ILogger<DatabaseManagementService> _logger;
        private readonly string _databaseManagerAPIBase;
        private readonly string _databaseManagerKey;

        public DatabaseManagementService(IHttpClientFactory clientFactory, ILogger<DatabaseManagementService> logger, IConfiguration configuration) : base(clientFactory)
        {
            _logger = logger;

            _databaseManagerAPIBase = configuration["DatabaseManagerAPI"]
                ?? throw new InvalidOperationException("DatabaseManagerAPI is not configured");

            _databaseManagerKey = configuration["DatabaseManagerKey"]
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
