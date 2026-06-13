using DatabaseManager.Services.DataTransfer.Extensions;
using DatabaseManager.Services.DataTransfer.Models;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;

namespace DatabaseManager.Services.DataTransfer.Services
{
    public class SqliteIndexTransferProvider : BaseService, IIndexDataTransferProvider
    {
        private readonly ILogger<SqliteIndexTransferProvider> _log;
        private readonly string _indexAPIBase;

        public SqliteIndexTransferProvider(ILogger<SqliteIndexTransferProvider> log, IHttpClientFactory httpClientFactory, IConfiguration configuration)
        : base(httpClientFactory)
        {
            _log = log;
            _indexAPIBase = configuration["IndexAPI"]
                ?? throw new InvalidOperationException("IndexAPI configuration is missing");
        }

        public async Task<IEnumerable<IndexModel>> GetIndexesWithDataType(string dataType, string connectionString)
        {
            string url = _indexAPIBase.BuildFunctionUrl("/api/indexes/search", $"dataType={dataType}", "");
            IEnumerable<IndexModel> response = await SendAsync<IEnumerable<IndexModel>>(new ApiRequest()
            {
                ApiType = SD.ApiType.GET,
                Url = url
            });

            return response;
        }

        public async Task<IndexModel> GetIndexRoot(string connectionString)
        {
            string url = _indexAPIBase.BuildFunctionUrl("/Index/1", "", "");
            ResponseDto response = await SendAsync<ResponseDto>(new ApiRequest()
            {
                ApiType = SD.ApiType.GET,
                Url = url
            });

            if (!response.IsSuccess)
                throw new InvalidOperationException(
                    $"Failed to get index root: {string.Join(", ", response.ErrorMessages)}");

            return JsonConvert.DeserializeObject<IndexModel>(Convert.ToString(response.Result))
                ?? throw new InvalidOperationException("Index root deserialized to null");
        }
    }
}
