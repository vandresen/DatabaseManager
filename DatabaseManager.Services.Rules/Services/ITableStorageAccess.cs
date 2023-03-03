using Azure;
using Azure.Data.Tables;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DatabaseManager.Services.Rules.Services
{
    public interface ITableStorageAccess
    {
        void SetConnectionString(string connection);
        Pageable<TableEntity> GetRecords(string container);
        TableEntity GetRecord(string container, string PartitionKey, string RowKey);
        void SaveRecord(string container, TableEntity entity);
        void UpdateRecord(string container, TableEntity entity);
        void Delete(string container, string PartitionKey, string RowKey);
    }
}
