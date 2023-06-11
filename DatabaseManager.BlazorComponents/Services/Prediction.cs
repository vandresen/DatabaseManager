using DatabaseManager.BlazorComponents.Extensions;
using DatabaseManager.Shared;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DatabaseManager.BlazorComponents.Services
{
    public class Prediction : IPrediction
    {
        private readonly IHttpService httpService;
        private string url;
        private string baseUrl;
        private readonly string apiKey;

        public Prediction(IHttpService httpService, SingletonServices settings)
        {
            this.httpService = httpService;
            baseUrl = settings.BaseUrl;
            apiKey = settings.ApiKey;
        }

        public async Task<List<PredictionCorrection>> GetResults(string source)
        {
            if (string.IsNullOrEmpty(baseUrl)) url = $"api/prediction/{source}";
            else url = baseUrl.BuildFunctionUrl("GetResults", $"name={source}", apiKey);
            Console.WriteLine(url);
            var response = await httpService.Get<List<PredictionCorrection>>(url);
            if (!response.Success)
            {
                throw new ApplicationException(await response.GetBody());
            }
            return response.Response;
        }

        public async Task ProcessPrediction(PredictionParameters predictionParams)
        {
            if (string.IsNullOrEmpty(baseUrl)) url = "api/prediction";
            else throw new ApplicationException("URL error");
            Console.WriteLine($"Execure Prediction URL: {url}");
            var response = await httpService.Post(url, predictionParams);
            if (!response.Success)
            {
                throw new ApplicationException(await response.GetBody());
            }
        }
    }
}
