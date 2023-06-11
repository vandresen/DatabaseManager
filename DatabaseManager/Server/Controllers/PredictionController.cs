using DatabaseManager.Common.Helpers;
using DatabaseManager.Shared;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace DatabaseManager.Server.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class PredictionController : ControllerBase
    {
        private string _connectionString;
        private readonly ILogger<PredictionController> logger;

        public PredictionController(IConfiguration configuration,
            ILogger<PredictionController> logger)
        {
            _connectionString = configuration.GetConnectionString("AzureStorageConnection");
            this.logger = logger;
        }

        [HttpGet("{source}")]
        public async Task<ActionResult<List<PredictionCorrection>>> Get(string source)
        {
            List<PredictionCorrection> predictionResuls = new List<PredictionCorrection>();
            try
            {
                GetStorageAccount();
                Predictions predict = new Predictions(_connectionString, logger);
                predictionResuls = await predict.GetPredictions(source);
            }
            catch (Exception ex)
            {
                return BadRequest(ex.ToString());
            }
            return predictionResuls;
        }


        [HttpPost]
        public async Task<ActionResult<string>> ExecutePrediction(PredictionParameters predictionParams)
        {
            try
            {
                if (predictionParams == null) return BadRequest();
                GetStorageAccount();
                Predictions predict = new Predictions(_connectionString, logger);
                await predict.ExecutePrediction(predictionParams);
            }
            catch (Exception ex)
            {
                return BadRequest(ex.ToString());
            }

            return Ok($"OK");
        }

        private void GetStorageAccount()
        {
            string tmpConnString = Request.Headers["AzureStorageConnection"];
            if (!string.IsNullOrEmpty(tmpConnString)) _connectionString = tmpConnString;
            if (string.IsNullOrEmpty(_connectionString))
            {
                Exception error = new Exception($"Azure storage key string is not set");
                throw error;
            }
        }
    }
}
