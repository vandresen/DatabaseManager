using DatabaseManager.Shared;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace DatabaseManager.Client.Helpers
{
    public class Prediction: IPrediction
    {
        private readonly IHttpService httpService;
        private string url = "api/Prediction";

        public Prediction(IHttpService httpService)
        {
            this.httpService = httpService;
        }

        public async Task<List<PredictionCorrection>> GetPredictions(string source)
        {
            var response = await httpService.Get<List<PredictionCorrection>>($"{url}/{source}");
            if (!response.Success)
            {
                throw new ApplicationException(await response.GetBody());
            }
            return response.Response;
        }

        public async Task ProcessPredictions(PredictionParameters predictionParams)
        {
            var response = await httpService.Post(url, predictionParams);
            if (!response.Success)
            {
                throw new ApplicationException(await response.GetBody());
            }
        }
    }
}
