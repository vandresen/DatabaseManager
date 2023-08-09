using DatabaseManager.Common.Helpers;
using DatabaseManager.Shared;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using System.Collections.Generic;
using System.Threading.Tasks;
using System;

namespace DatabaseManager.Server.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class SyncController : ControllerBase
    {
        private readonly ILogger<SyncController> _logger;
        private string _connectionString;

        public SyncController(ILogger<SyncController> logger)
        {
            _logger = logger;
        }

        [HttpGet("{source}")]
        public async Task<ActionResult<List<PredictionCorrection>>> Get(string source)
        {
            List<PredictionCorrection> predictionResuls = new List<PredictionCorrection>();
            try
            {
                GetStorageAccount();
                Predictions predict = new Predictions(_connectionString, _logger);
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
                Predictions predict = new Predictions(_connectionString, _logger);
                await predict.SyncPredictions(predictionParams);
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
                _logger.LogError("Azure storage key string is not set");
                Exception error = new Exception($"Azure storage key string is not set");
                throw error;
            }
        }
    }
}
