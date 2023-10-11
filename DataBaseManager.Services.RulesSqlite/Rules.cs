using System.Net;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;
using DatabaseManager.Services.RulesSqlite.Models;
using DatabaseManager.Services.RulesSqlite.Services;
using Azure;
using System;
using System.Threading.Tasks;
using System.Collections.Generic;

namespace DatabaseManager.Services.RulesSqlite
{
    public class Rules
    {
        private readonly ILogger _logger;
        private readonly IRuleAccess _ra;
        private protected ResponseDto _response;
        private string databaseName = "mydatabase.db";

        public Rules(ILoggerFactory loggerFactory, IRuleAccess ra)
        {
            _logger = loggerFactory.CreateLogger<Rules>();
            _response = new ResponseDto();
            _ra = ra;
        }

        [Function("CreateDatabase")]
        public async Task<ResponseDto> CreateDatabase([HttpTrigger(AuthorizationLevel.Function, "post")] HttpRequestData req)
        {
            _logger.LogInformation("Rules CreateDatabase: Starting.");
            try
            {
                await _ra.CreateDatabaseRules();
                _response.IsSuccess = true;
            }
            catch (Exception ex)
            {
                _response.ErrorMessages
                     = new List<string>() { "CreateDatabaseRules: " + ex.ToString() };
                _logger.LogError($"CreateDatabaseRules: {ex}");
            }
            _logger.LogInformation("Rules CreateDatabase: Complete.");
            return _response;
        }

        [Function("CreateStandardRules")]
        public async Task<ResponseDto> CreateStandardRules([HttpTrigger(AuthorizationLevel.Function, "post")] HttpRequestData req)
        {
            _logger.LogInformation("Rules CreateStandardRules: Starting.");
            try
            {
                await _ra.InitializeStandardRules();
                _response.IsSuccess = true;
            }
            catch (Exception ex)
            {
                _response.IsSuccess = false;
                _response.ErrorMessages
                     = new List<string>() { "CreateStandardRules: " + ex.ToString() };
                _logger.LogError($"CreatestandardRules: {ex}");
            }
            _logger.LogInformation("Rules CreateStandardRules: Complete.");
            return _response;
        }
    }
}
