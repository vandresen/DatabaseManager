using Azure;
using Azure.Data.Tables;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DatabaseManager.Services.DataOpsManagement.Services
{
    public interface ITableAccess
    {
        void SetConnectionString(string connection);
        Pageable<TableEntity> GetRecords(string container);
        TableEntity GetRecord(string container, string PartitionKey, string RowKey);
    }
}
