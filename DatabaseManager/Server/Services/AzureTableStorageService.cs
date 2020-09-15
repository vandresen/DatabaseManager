using DatabaseManager.Server.Entities;
using DatabaseManager.Shared;
using Microsoft.Azure.Cosmos.Table;
using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace DatabaseManager.Server.Services
{
    public class AzureTableStorageService : ITableStorageService
    {
        private string connectionString;

        public AzureTableStorageService(IConfiguration configuration)
        {
            connectionString = configuration.GetConnectionString("AzureStorageConnection");
        }

        public void SetConnectionString(string connection)
        {
            if (!string.IsNullOrEmpty(connection)) connectionString = connection;
            if (string.IsNullOrEmpty(connectionString))
            {
                Exception error = new Exception($"Connection string is not set");
                throw error;
            }
        }

        public async Task<ConnectParameters> GetTable(string container, string name)
        {
            ConnectParameters connector = new ConnectParameters();
            CloudTable table = GetTableConnect(connectionString, container);
            TableOperation retrieveOperation = TableOperation.Retrieve<SourceEntity>("PPDM", name);
            TableResult result = await table.ExecuteAsync(retrieveOperation);
            SourceEntity entity = result.Result as SourceEntity;
            if (entity == null) 
            {
                Exception error = new Exception($"Source name is empty");
                throw error;
            }
            connector.SourceName = name;
            connector.Database = entity.DatabaseName;
            connector.DatabaseServer = entity.DatabaseServer;
            connector.DatabaseUser = entity.User;
            connector.DatabasePassword = entity.Password;
            connector.ConnectionString = entity.ConnectionString;
            return connector;
        }

        public async Task<List<ConnectParameters>> ListTable(string container, string dataAccessDef)
        {
            List<ConnectParameters> connectors = new List<ConnectParameters>();
            CloudTable table = GetTableConnect(connectionString, container);
            TableQuery<SourceEntity> tableQuery = new TableQuery<SourceEntity>().
                Where(TableQuery.GenerateFilterCondition("PartitionKey", QueryComparisons.Equal, "PPDM"));
            foreach (SourceEntity entity in table.ExecuteQuery(tableQuery))
            {
                connectors.Add(new ConnectParameters()
                {
                    SourceName = entity.RowKey,
                    Database = entity.DatabaseName,
                    DatabaseServer = entity.DatabaseServer,
                    DatabasePassword = entity.Password,
                    ConnectionString = entity.ConnectionString,
                    DatabaseUser = entity.User,
                    DataAccessDefinition = dataAccessDef
                });
            }
            return connectors;
        }

        public async Task<List<T>> GetTableRecords<T>(string container) where T : ITableEntity, new()
        {
            List<T> data = new List<T>();
            CloudTable table = GetTableConnect(connectionString, container);
            TableQuery<T> tableQuery = new TableQuery<T>().
                Where(TableQuery.GenerateFilterCondition("PartitionKey", QueryComparisons.Equal, "PPDM"));
            data = table.ExecuteQuery(tableQuery).ToList();
            return data;
        }

        public async Task<T> GetTableRecord<T>(string container, string name) where T : ITableEntity, new()
        {
            CloudTable table = GetTableConnect(connectionString, container);
            TableOperation retrieveOperation = TableOperation.Retrieve<T>("PPDM", name);
            TableResult result = await table.ExecuteAsync(retrieveOperation);
            T data = (T)result.Result;
            return data;
        }

        public async Task SaveTable(string container, ConnectParameters connectParameters)
        {
            CloudTable table = GetTableConnect(connectionString, container);
            await table.CreateIfNotExistsAsync();
            string name = connectParameters.SourceName;
            if (String.IsNullOrEmpty(name))
            {
                Exception error = new Exception($"Source name is empty");
                throw error;
            }
            SourceEntity sourceEntity = new SourceEntity(name)
            {
                DatabaseName = connectParameters.Database,
                DatabaseServer = connectParameters.DatabaseServer,
                User = connectParameters.DatabaseUser,
                Password = connectParameters.DatabasePassword,
                ConnectionString = connectParameters.ConnectionString
            };
            TableOperation insertOperation = TableOperation.Insert(sourceEntity);
            await table.ExecuteAsync(insertOperation);
        }

        public async Task SaveTableRecord<T>(string container, string name, T data) where T: TableEntity
        {
            CloudTable table = GetTableConnect(connectionString, container);
            await table.CreateIfNotExistsAsync();
            if (String.IsNullOrEmpty(name))
            {
                Exception error = new Exception($"Name is empty");
                throw error;
            }
            TableOperation insertOperation = TableOperation.Insert(data);
            await table.ExecuteAsync(insertOperation);
        }

        public async Task UpdateTable(string container, ConnectParameters connectParameters)
        {
            CloudTable table = GetTableConnect(connectionString, container);
            string name = connectParameters.SourceName;
            if (String.IsNullOrEmpty(name))
            {
                Exception error = new Exception($"Source name is empty");
                throw error;
            }
            SourceEntity sourceEntity = new SourceEntity(name)
            {
                DatabaseName = connectParameters.Database,
                DatabaseServer = connectParameters.DatabaseServer,
                User = connectParameters.DatabaseUser,
                Password = connectParameters.DatabasePassword,
                ConnectionString = connectParameters.ConnectionString
            };
            TableOperation insertOrMergeOperation = TableOperation.InsertOrMerge(sourceEntity);
            TableResult result = await table.ExecuteAsync(insertOrMergeOperation);
        }

        public async Task DeleteTable(string container, string name)
        {
            CloudTable table = GetTableConnect(connectionString, container);
            TableOperation retrieveOperation = TableOperation.Retrieve<SourceEntity>("PPDM", name);
            TableResult result = await table.ExecuteAsync(retrieveOperation);
            SourceEntity entity = result.Result as SourceEntity;
            if (entity == null)
            {
                Exception error = new Exception($"No source with name {name}");
                throw error;
            }

            TableOperation deleteOperation = TableOperation.Delete(entity);
            result = await table.ExecuteAsync(deleteOperation);
        }

        private CloudTable GetTableConnect(string connectionString, string tableName)
        {
            CloudStorageAccount account = CloudStorageAccount.Parse(connectionString);
            CloudTableClient client = account.CreateCloudTableClient();
            CloudTable table = client.GetTableReference(tableName);
            return table;
        }
    }
}
