using Azure;
using Azure.Data.Tables;
using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DatabaseManager.Services.Rules.Services
{
    public class AzureTableStorageAccess : ITableStorageAccess
    {
        private string _connectionString;

        public AzureTableStorageAccess(IConfiguration configuration)
        {
            _connectionString = configuration.GetConnectionString("AzureStorageConnection");
        }

        public void Delete(string container, string PartitionKey, string RowKey)
        {
            TableClient tableClient = new TableClient(_connectionString, container);
            tableClient.DeleteEntity(PartitionKey, RowKey);
        }

        public TableEntity GetRecord(string container, string PartitionKey, string RowKey)
        {
            TableClient tableClient = new TableClient(_connectionString, container);
            TableEntity entity = tableClient.GetEntity<TableEntity>(PartitionKey, RowKey).Value;
            return entity;
        }

        public Pageable<TableEntity> GetRecords(string container)
        {
            TableClient tableClient = new TableClient(_connectionString, container);
            Pageable<TableEntity> entities = tableClient.Query<TableEntity>();
            return entities;
        }

        public void SaveRecord(string container, TableEntity entity)
        {
            TableClient tableClient = new TableClient(_connectionString, container);
            tableClient.AddEntity(entity);
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

        public void UpdateRecord(string container, TableEntity entity)
        {
            TableClient tableClient = new TableClient(_connectionString, container);
            tableClient.UpdateEntity(entity, ETag.All, TableUpdateMode.Replace);
        }
    }
}
