using DatabaseManager.ServerLessClient.Helpers;
using DatabaseManager.ServerLessClient.Models;

namespace DatabaseManager.ServerLessClient.Services
{
    public class DatabaseManagementService : BaseService, IDatabaseManagementService
    {
        private readonly ILogger<DatabaseManagementService> _logger;
        private readonly string _databaseManagerAPIBase;
        private readonly string _databaseManagerKey;
        private readonly string _indexAPIBase;
        private readonly BlazorSingletonService _settings;

        public DatabaseManagementService(IHttpClientFactory clientFactory, ILogger<DatabaseManagementService> logger, 
            IConfiguration configuration, BlazorSingletonService settings) : base(clientFactory)
        {
            _logger = logger;
            _settings = settings;
            _indexAPIBase = configuration["ServiceUrls:IndexAPI"]
                ?? throw new InvalidOperationException("Missing ServiceUrls:IndexAPI");
            _databaseManagerAPIBase = SD.DatabaseManagerAPIBase
                ?? throw new InvalidOperationException("DatabaseManagerAPI is not configured");
            _databaseManagerKey = SD.DatabaseManagerKey
                ?? throw new InvalidOperationException("DatabaseManagerKey is not configured");
        }

        public async Task Create(DataModelParameters modelParameters)
        {
            string url = _databaseManagerAPIBase.BuildFunctionUrl($"/Create", "", _databaseManagerKey);
            Console.WriteLine($"Create data mode: url = {url}");
            ResponseDto response = await this.SendAsync<ResponseDto>(new ApiRequest()
            {
                ApiType = SD.ApiType.POST,
                AzureStorage = _settings.AzureStorage,
                Url = url,
                Data = modelParameters
            });
            if (!response.IsSuccess)
            {

                Console.WriteLine(String.Join("There is a problem creating the data model; ", response.ErrorMessages));
                throw new ApplicationException(String.Join("There is a problem creating the data model; ", response.ErrorMessages));
            }
        }

        public async Task CreateSqlite()
        {
            string url = _indexAPIBase.BuildFunctionUrl("/CreateDatabase", "", "");
            _logger.LogInformation($"Creating SQLite model from url {url}");
            ResponseDto response = await this.SendAsync<ResponseDto>(new ApiRequest()
            {
                ApiType = SD.ApiType.POST,
                Url = url
            });
            if (!response.IsSuccess)
            {

                Console.WriteLine(String.Join("There is a problem creating the SqlLite index; ", response.ErrorMessages));
                throw new ApplicationException(String.Join("There is a problem creating the SqlLite index; ", response.ErrorMessages));
            }
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
