using Azure.Storage.Queues;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DatabaseManager.Services.DataTransfer.Services
{
    public class AzureQueueService : IQueueService
    {
        private string _connectionString;

        public AzureQueueService()
        {

        }

        public async Task<string> GetMessage(string queueName)
        {
            string message = "";
            QueueClient queueClient = new QueueClient(_connectionString, queueName);
            var queueMessage = await queueClient.ReceiveMessageAsync();
            if (queueMessage.Value != null)
            {
                message = queueMessage.Value.MessageText;
                await queueClient.DeleteMessageAsync(queueMessage.Value.MessageId, queueMessage.Value.PopReceipt);
            }
            return message;
        }

        public async Task InsertMessage(string queueName, string message)
        {
            QueueClient queueClient = new QueueClient(_connectionString, queueName);

            queueClient.CreateIfNotExists();

            if (queueClient.Exists())
            {
                queueClient.SendMessage(message);
            }
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
    }
}
