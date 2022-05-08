using Azure.Storage.Queues;
using Azure.Storage.Queues.Models;
using DatabaseManager.Common.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace DatabaseManager.LocalDataTransfer
{
    public class Worker : BackgroundService
    {
        private readonly ILogger<Worker> _logger;
        private readonly AppSettings _appSettings;
        string queueName = "datatransferqueue";
        string infoName = "datatransferinfo";

        public Worker(ILogger<Worker> logger, AppSettings appSettings)
        {
            _logger = logger;
            _appSettings = appSettings;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            int counter = 100;
            string infoMessage = "";
            var builder = new ConfigurationBuilder();
            IConfiguration configuration = builder.Build();
            IQueueService queueService = new AzureQueueServiceCommon(configuration);
            queueService.SetConnectionString(_appSettings.StorageAccount);
            while (!stoppingToken.IsCancellationRequested)
            {
                counter++;

                if (counter > 100)
                {
                    _logger.LogInformation("Worker running at: {time}", DateTimeOffset.Now);
                    counter = 0;
                }

                string message = queueService.GetMessage(queueName);
                if (!string.IsNullOrEmpty(message))
                {
                    DataTransfer trans = new DataTransfer(_logger, _appSettings, queueService);

                    await trans.GetTransferConnector(message);
                    infoMessage = "Start deleting tables";
                    queueService.InsertMessage(infoName, infoMessage);
                    trans.DeleteTables();
                    _logger.LogInformation($"Tables deleted");
                    infoMessage = "Tables deleted";
                    queueService.InsertMessage(infoName, infoMessage);

                    trans.CopyTables();
                    _logger.LogInformation($"Tables copied");
                    infoMessage = "Complete";
                    queueService.InsertMessage(infoName, infoMessage);
                }

                await Task.Delay(30000, stoppingToken);
            }
        }
    }
}
