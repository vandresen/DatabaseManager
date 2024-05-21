using Azure;
using Azure.Data.Tables;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DatabaseManager.Services.DataOpsManagement.Services
{
    public class AzureTableAccess : ITableAccess
    {
        private string _connectionString;

        public TableEntity GetRecord(string container, string PartitionKey, string RowKey)
        {
            throw new NotImplementedException();
        }

        public Pageable<TableEntity> GetRecords(string container)
        {
            throw new NotImplementedException();
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
