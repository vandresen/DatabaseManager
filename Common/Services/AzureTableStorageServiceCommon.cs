using DatabaseManager.Common.Entities;
using Microsoft.Azure.Cosmos.Table;
using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DatabaseManager.Common.Services
{
    public class AzureTableStorageServiceCommon : ITableStorageServiceCommon
    {
        private string connectionString;

        public AzureTableStorageServiceCommon(IConfiguration configuration)
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
            T data = new T();
            CloudTable table = GetTableConnect(connectionString, container);
            TableOperation retrieveOperation = TableOperation.Retrieve<T>("PPDM", name);
            TableResult result = await table.ExecuteAsync(retrieveOperation);
            data = (T)result.Result;
            return data;
        }

        public async Task SaveTableRecord<T>(string container, string name, T data) where T : TableEntity
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

        public async Task UpdateTable<T>(string container, T data) where T : TableEntity
        {
            CloudTable table = GetTableConnect(connectionString, container);
            TableOperation insertOrMergeOperation = TableOperation.InsertOrMerge(data);
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
