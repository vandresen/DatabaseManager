using System.Net;
using System.Security.Cryptography;
using System.Text;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using PredictGroundElevation.Models;

namespace PredictGroundElevation
{
    public class PredictionGroundElevation
    {
        private readonly ILogger _logger;

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
                string lat = dataObject["SURFACE_LATITUDE"].ToString();
                string lon = dataObject["SURFACE_LONGITUDE"].ToString();
                if (lat == "-99999.0" && lon == "-99999.0")
                {
                    _logger.LogError("Can't find a good location");
                    throw new Exception("Can't find a good location");
                }

                // Open-Meteo does not require an API key
                HttpClient client = new HttpClient();
                string elevationUrl = $"https://api.open-meteo.com/v1/elevation?latitude={lat}&longitude={lon}";

                _logger.LogInformation($"Calling elevation API: {elevationUrl}");

                HttpResponseMessage elevationResponse = await client.GetAsync(elevationUrl);
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
                    _logger.LogError($"Elevation API returned status: {elevationResponse.StatusCode}");
                    throw new Exception($"Elevation API returned status: {elevationResponse.StatusCode}");
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
