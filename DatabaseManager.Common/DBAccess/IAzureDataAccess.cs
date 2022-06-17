using Azure;
using Azure.Data.Tables;
using System.Threading.Tasks;

namespace DatabaseManager.Common.DBAccess
{
    public interface IAzureDataAccess
    {
        void SetConnectionString(string connection);
        Pageable<TableEntity> GetRecords(string container);
        TableEntity GetRecord(string container, string PartitionKey, string RowKey);
        void SaveRecord(string container, TableEntity entity);
        void UpdateRecord(string container, TableEntity entity);
        void Delete(string container, string PartitionKey, string RowKey);
    }
}
