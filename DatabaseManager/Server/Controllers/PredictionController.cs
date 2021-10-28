using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Net.Http;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using AutoMapper;
using DatabaseManager.Common.Helpers;
using DatabaseManager.Server.Entities;
//using DatabaseManager.Common.Helpers;
//using DatabaseManager.Server.Helpers;
using DatabaseManager.Server.Services;
using DatabaseManager.Shared;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.VisualStudio.Web.CodeGeneration.EntityFrameworkCore;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace DatabaseManager.Server.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class PredictionController : ControllerBase
    {
        private string connectionString;
        private readonly ILogger<PredictionController> logger;

        public PredictionController(IConfiguration configuration,
            ILogger<PredictionController> logger)
        {
            connectionString = configuration.GetConnectionString("AzureStorageConnection");
            this.logger = logger;
        }

        [HttpGet("{source}")]
        public async Task<ActionResult<List<PredictionCorrection>>> Get(string source)
        {
            List<PredictionCorrection> predictionResuls = new List<PredictionCorrection>();
            try
            {
                string tmpConnString = Request.Headers["AzureStorageConnection"];
                Predictions predict = new Predictions(tmpConnString);
                predictionResuls = await predict.GetPredictions(source);
            }
            catch (Exception ex)
            {
                return BadRequest(ex.ToString());
            }
            return predictionResuls;
        }

        [HttpGet("{source}/{id}")]
        public async Task<ActionResult<string>> GetPredictions(string source, int id)
        {
            string result = "[]";
            try
            {
                string tmpConnString = Request.Headers["AzureStorageConnection"];
                Predictions predict = new Predictions(tmpConnString);
                List<DmsIndex> qcIndex = new List<DmsIndex>();
                qcIndex = await predict.GetPrediction(source, id);
                result = JsonConvert.SerializeObject(qcIndex);
            }
            catch (Exception)
            {
                return BadRequest();
            }
            return result;
        }

        [HttpPost]
        public async Task<ActionResult<string>> ExecutePrediction(PredictionParameters predictionParams)
        {
            try
            {
                if (predictionParams == null) return BadRequest();
                string tmpConnString = Request.Headers["AzureStorageConnection"];
                Predictions predict = new Predictions(tmpConnString);
                await predict.ExecutePrediction(predictionParams);
            }
            catch (Exception ex)
            {
                return BadRequest(ex.ToString());
            }

            return Ok($"OK");
        }
    }
}
