using DatabaseManager.Services.DataTransfer.Models;
using Microsoft.Extensions.Logging;

namespace DatabaseManager.Services.DataTransfer.Services
{
    public class SqlServerIndexTransferProvider : IIndexDataTransferProvider
    {
        private readonly ILogger<SqlServerIndexTransferProvider> _log;
        private readonly IDatabaseAccess _dbAccess;
        private string getSql = "Select IndexId, IndexNode.ToString() AS TextIndexNode, " +
            "IndexLevel, DataName, DataType, DataKey, QC_String, UniqKey, JsonDataObject, " +
            "Latitude, Longitude " +
            "from pdo_qc_index";

        public SqlServerIndexTransferProvider(ILogger<SqlServerIndexTransferProvider> log)
        {
            _log = log;
            _dbAccess = new DatabaseAccess();
        }

        public async Task<IEnumerable<IndexModel>> GetIndexesWithDataType(string dataType, string connectionString)
        {
            string sql = getSql + $" WHERE DATATYPE = '{dataType}'";
            IEnumerable<IndexModel> result = await _dbAccess.ReadData<IndexModel>(sql, connectionString);

            return result;
        }

        public async Task<IndexModel> GetIndexRoot(string connectionString)
        {
            var results = await _dbAccess.LoadData<IndexModel, dynamic>("dbo.spGetIndexWithINDEXNODE", new { query = '/' }, connectionString);
            return results?.FirstOrDefault()
                ?? throw new InvalidOperationException($"No index root found in database at: {connectionString}");
        }
    }
}
