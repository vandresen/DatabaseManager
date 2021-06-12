using Azure.Storage.Queues;
using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace DatabaseManager.Server
{
    public class AzureQueueService : IQueueService
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
    }
}
