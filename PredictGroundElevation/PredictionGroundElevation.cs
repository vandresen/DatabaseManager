using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using PredictGroundElevation.Models;
using System.Net;

namespace PredictGroundElevation
{
    public class PredictionGroundElevation
    {
        private readonly ILogger _logger;
        private static readonly HttpClient _httpClient = new HttpClient();
        private static int maxRetries = 3;

        public PredictionGroundElevation(ILoggerFactory loggerFactory)
        {
            _logger = loggerFactory.CreateLogger<PredictionGroundElevation>();
        }

        [Function("PredictionGroundElevation")]
        public async Task<HttpResponseData> Run([HttpTrigger(AuthorizationLevel.Function, "post")] HttpRequestData req)
        {
            _logger.LogInformation("PredictGroundElevationUsingElevation: Started.");
            Result result = new Result
            {
                Status = "Failed"
            };

            try
            {
                var stringBody = await new StreamReader(req.Body).ReadToEndAsync();
                InputParameters inputParms = JsonConvert.DeserializeObject<InputParameters>(Convert.ToString(stringBody));

                if (string.IsNullOrEmpty(inputParms.DataObject))
                {
                    _logger.LogError("Data Object is missing");
                    throw new Exception("Data Object is missing");
                }

                // Get location data
                JObject dataObject = JObject.Parse(inputParms.DataObject);
                string lat = dataObject["SURFACE_LATITUDE"].ToString().Trim();
                string lon = dataObject["SURFACE_LONGITUDE"].ToString().Trim();
                if (lat == "-99999.0" && lon == "-99999.0")
                {
                    _logger.LogError("Can't find a good location");
                    throw new Exception("Can't find a good location");
                }

                // Open-Meteo does not require an API key
                string elevationUrl = $"https://api.open-meteo.com/v1/elevation?latitude={lat}&longitude={lon}";
                _logger.LogInformation($"Calling elevation API: {elevationUrl}");

                // Retry logic
                HttpResponseMessage elevationResponse = null;
                for (int i = 0; i < maxRetries; i++)
                {
                    elevationResponse = await _httpClient.GetAsync(elevationUrl);
                    if (elevationResponse.StatusCode == HttpStatusCode.OK)
                        break;

                    _logger.LogWarning($"Attempt {i + 1} of {maxRetries} failed with {elevationResponse.StatusCode}, retrying...");
                    await Task.Delay(1100);
                }

                if (elevationResponse.StatusCode == HttpStatusCode.OK)
                {
                    using (HttpContent respContent = elevationResponse.Content)
                    {
                        string tr = await respContent.ReadAsStringAsync();
                        _logger.LogInformation(tr);

                        // Open-Meteo returns: {"elevation":[1607.0]}
                        JObject root = JObject.Parse(tr);
                        JArray elevations = (JArray)root["elevation"];
                        string strElevation = elevations[0].ToString();
                        _logger.LogInformation(strElevation);

                        // Convert meters to feet
                        double ftElevation = Convert.ToDouble(strElevation) * 3.28084;
                        dataObject["GROUND_ELEV"] = ftElevation;

                        string remark = dataObject["REMARK"] + ";Ground elevation calculated by Open-Meteo;";
                        dataObject["REMARK"] = remark;
                        result.DataObject = dataObject.ToString();
                        result.DataType = "WellBore";
                        result.SaveType = "Update";
                        result.IndexId = inputParms.IndexId;
                        result.Status = "Passed";
                    }
                }
                else
                {
                    _logger.LogError($"Elevation API failed after {maxRetries} attempts, last status: {elevationResponse.StatusCode}");
                    throw new Exception($"Elevation API failed after {maxRetries} attempts, last status: {elevationResponse.StatusCode}");
                }

                var response = req.CreateResponse(HttpStatusCode.OK);
                response.Headers.Add("Content-Type", "application/json");
                string jsonResult = JsonConvert.SerializeObject(result);
                response.WriteString(jsonResult);
                return response;
            }
            catch (Exception ex)
            {
                _logger.LogError($"PredictionGroundElevation: Error processing prediction: {ex}");
                var response = req.CreateResponse(HttpStatusCode.NotAcceptable);
                response.Headers.Add("Content-Type", "text/plain; charset=utf-8");
                response.WriteString($"PredictionGroundElevation: Error processing prediction, {ex}");
                return response;
            }
        }
    }
}
