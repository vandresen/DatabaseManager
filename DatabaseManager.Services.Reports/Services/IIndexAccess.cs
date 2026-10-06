using DatabaseManager.Services.Reports.Models;

namespace DatabaseManager.Services.Reports.Services
{
    public interface IIndexAccess
    {
        Task<T> GetIndexFailures<T>(string dataSource, string project, string dataType, string qcString);
        Task<T> GetFailureCounts<T>(string dataSource, string project);
        Task<T> GetRootIndex<T>(string dataSource, string project);
        Task InsertEdits(ReportData reportData, string dataSource, string project);
        Task<T> DeleteEdits<T>(int id, string dataSource, string project);
        Task InsertChildEdits(ReportData reportData, string dataSource, string project);

        Task MergeIndexes(ReportData reportData, string dataSource, string project);
    }
}
