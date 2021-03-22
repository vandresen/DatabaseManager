using Azure.Storage.Queues;
using System;
using System.Collections.Generic;
using System.Text;

namespace DatabaseManager.AppFunctions.Helpers
{
    public class Utilities
    {
        public static void InsertMessage(string queueName, string message, string connectionString)
        {
            QueueClient queueClient = new QueueClient(connectionString, queueName);

            queueClient.CreateIfNotExists();

            if (queueClient.Exists())
            {
                queueClient.SendMessage(message);
            }
        }
    }
}
