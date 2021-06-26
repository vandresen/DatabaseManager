using Azure.Storage.Queues;
using Azure.Storage.Queues.Models;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DatabaseManager.Components.Services
{
    public class AzureQueueService: IQueueService
    {
        private string _connectionString;

        public AzureQueueService(IConfiguration configuration)
        {
            _connectionString = configuration.GetConnectionString("AzureStorageConnection");
        }

        public void SetConnectionString(string connection)
        {
            if (!string.IsNullOrEmpty(connection)) _connectionString = connection;
            if (string.IsNullOrEmpty(_connectionString))
            {
                Exception error = new Exception($"Connection string is not set");
                throw error;
            }
        }

        public void InsertMessage(string queueName, string message)
        {
            QueueClient queueClient = new QueueClient(_connectionString, queueName);

            queueClient.CreateIfNotExists();

            if (queueClient.Exists())
            {
                queueClient.SendMessage(message);
            }
        }
        
        public string GetMessage(string queueName)
        {
            string messageText = "";
            QueueClient queueClient = new QueueClient(_connectionString, queueName);
            if (queueClient.Exists())
            {
                QueueProperties properties = queueClient.GetProperties();
                int cachedMessagesCount = properties.ApproximateMessagesCount;
                //_logger.LogInformation($"Number of queue messages {cachedMessagesCount}");
                if (cachedMessagesCount > 0)
                {
                    QueueMessage[] messages = queueClient.ReceiveMessages();
                    //_logger.LogInformation($"Message length {messages.Length}");
                    foreach (var message in messages)
                    {
                        messageText = message.MessageText;
                        //_logger.LogInformation($"Message: '{messageText}', {DateTimeOffset.Now}");
                        queueClient.DeleteMessage(message.MessageId, message.PopReceipt);
                    }
                }
            }
            return messageText;
        }
    }
}
