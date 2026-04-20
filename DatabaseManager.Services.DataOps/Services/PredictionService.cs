using DatabaseManager.Services.DataOps.Extensions;
using DatabaseManager.Services.DataOps.Models;
using Microsoft.Extensions.Configuration;

namespace DatabaseManager.Services.DataOps.Services
{
    public class PredictionService : BaseService, IPredictionService
    {
        private readonly IConfiguration _configuration;

        public PredictionService(IConfiguration configuration, IHttpClientFactory clientFactory) : base(clientFactory)
        {
            _configuration = configuration;
        }

        public async Task<T> ProcessPrediction<T>(PredictionParameters predictionParameter)
        {
            var predictionAPIBase = _configuration.GetValue<string>("PredictionAPI");
            var predictionKey = _configuration.GetValue<string>("PredictionKey");
            string url = predictionAPIBase.BuildFunctionUrl($"/Predictions", $"", predictionKey);
            return await this.SendAsync<T>(new ApiRequest()
            {
                ApiType = SD.ApiType.POST,
                Url = url,
                Data = predictionParameter,
            });
        }
    }
}
