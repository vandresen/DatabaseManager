using Azure.Storage.Queues;
using Azure.Storage.Queues.Models;
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

        public Worker(ILogger<Worker> logger, AppSettings appSettings)
        {
            _logger = logger;
            _appSettings = appSettings;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            int counter = 100;
            while (!stoppingToken.IsCancellationRequested)
            {
                counter++;

                if (counter > 100)
                {
                    _logger.LogInformation("Worker running at: {time}", DateTimeOffset.Now);
                    counter = 0;
                }
                QueueClient queueClient = new QueueClient(_appSettings.StorageAccount, queueName);
                if (queueClient.Exists())
                {
                    QueueProperties properties = queueClient.GetProperties();
                    int cachedMessagesCount = properties.ApproximateMessagesCount;
                    //_logger.LogInformation($"Number of queue messages {cachedMessagesCount}");
                    if (cachedMessagesCount > 0)
                    {
                        QueueMessage[] messages = queueClient.ReceiveMessages();
                        _logger.LogInformation($"Message length {messages.Length}");
                        foreach (var message in messages)
                        {
                            _logger.LogInformation($"Message: '{message.MessageText}', {DateTimeOffset.Now}");
                            queueClient.DeleteMessage(message.MessageId, message.PopReceipt);
                            DataTransfer trans = new DataTransfer(_logger, _appSettings);
                            trans.GetTransferConnector(message.MessageText);
                            trans.DeleteTables();
                            trans.CopyTables();
                        }
                    }
                }

                await Task.Delay(30000, stoppingToken);
            }
        }
    }
}
