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
                    Exception error = new Exception($"Data Object is missing");
                    throw error;
                }

                //Get location data
                JObject dataObject = JObject.Parse(inputParms.DataObject);
                string lat = dataObject["SURFACE_LATITUDE"].ToString();
                string lon = dataObject["SURFACE_LONGITUDE"].ToString();
                if (lat == "-99999.0" && lon == "-99999.0")
                {
                    _logger.LogError("Can't find a good location");
                    Exception error = new Exception($"Can't find a good location");
                    throw error;
                }

                HttpClient Client = new HttpClient();
                string elevationKey = Environment.GetEnvironmentVariable("ElevationKey");
                string elevationApi = Environment.GetEnvironmentVariable("ElevationApi");
                string elevationUrl = elevationApi + lat + "," + lon + @"&key=" + elevationKey;
                if (string.IsNullOrEmpty(elevationUrl)) 
                {
                    _logger.LogError("URL for elevation api is missing");
                    Exception error = new Exception($"URL for elevation api is missing");
                    throw error;
                }
                StringContent content = new StringContent("", Encoding.UTF8, "application/json");
                HttpResponseMessage elevationResponse = Client.PostAsync(elevationUrl, content).Result;
                if(elevationResponse.StatusCode == HttpStatusCode.OK)
                {
                    using (HttpContent respContent = elevationResponse.Content)
                    {
                        string tr = respContent.ReadAsStringAsync().Result;
                        _logger.LogInformation(tr);
                        JObject root = JObject.Parse(tr);
                        string elevationsToken = root.SelectToken("resourceSets[0].resources[0].elevations").ToString();
                        JArray elevations = JArray.Parse(elevationsToken);
                        string strElevation = elevations[0].ToString();
                        _logger.LogInformation(strElevation);
                        double ftElevation = Convert.ToDouble(strElevation) * 3.28084;
                        dataObject["GROUND_ELEV"] = ftElevation;
                        //dataObject["DEPTH_DATUM"] = "GL";
                        string remark = dataObject["REMARK"] + ";Ground elevation calculated by Bing;";
                        dataObject["REMARK"] = remark;
                        result.DataObject = dataObject.ToString();
                        result.DataType = "WellBore";
                        result.SaveType = "Update";
                        result.IndexId = inputParms.IndexId;
                        result.Status = "Passed";
                    }
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
