using Azure.Storage.Queues;
using DatabaseManager.AppFunctions.Entities;
using Microsoft.AspNetCore.Http;
using Microsoft.Azure.WebJobs;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
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

        public static ConfigurationInfo GetConfigurations(ExecutionContext context)
        {
            ConfigurationInfo configInfo = new ConfigurationInfo();

            var config = new ConfigurationBuilder()
                    .SetBasePath(context.FunctionAppDirectory)
                    .AddJsonFile("local.settings.json", optional: true, reloadOnChange: true)
                    .AddEnvironmentVariables()
                    .Build();
            configInfo.DataOpsQueue = config["DataOpsQueue"];
            if (string.IsNullOrEmpty(configInfo.DataOpsQueue)) configInfo.ErrorsMessage = "Missing file storage connection";

            return configInfo;
        }

        public static string GetAzureStorageConnection(IHeaderDictionary headers, ILogger log)
        {
            string connection = headers.FirstOrDefault(x => x.Key == "azurestorageconnection").Value;
            if (string.IsNullOrEmpty(connection))
            {
                string errorMessage = "Error, missing azure storage account";
                log.LogError(errorMessage);
                Exception error = new Exception(errorMessage);
                throw error;
            }
            return connection;
        }
    }
}
