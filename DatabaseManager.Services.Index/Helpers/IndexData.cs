using DatabaseManager.Services.Index.Extensions;
using DatabaseManager.Services.Index.Models;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DatabaseManager.Services.Index.Helpers
{
    public class IndexData
    {
        private readonly ILogger _logger;

        public IndexData(ILogger logger)
        {
            _logger = logger;
        }

        public List<IndexDto> GetIndexes(HttpRequestData req)
        {
            List<IndexDto> indexes = new List<IndexDto>();
            string storageAccount = req.GetStorageKey();

            _logger.LogInformation($"Inside GetIndexes helper, {storageAccount}");

            return indexes;
        }
    }
}
