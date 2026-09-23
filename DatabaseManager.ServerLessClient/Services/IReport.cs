using DatabaseManager.ServerLessClient.Models;

namespace DatabaseManager.ServerLessClient.Services
{
    public interface IReport
    {
        Task<List<QcResult>> GetResults(string source);
        //Task<List<TableSchema>> GetAttributeInfo(string source, string dataType);
        Task<List<DmsIndex>> GetResult(string source, int id);
        Task Update(string source, ReportData reportData);
        Task Delete(int id, string source);
        Task InsertChild(string source, ReportData reportData);
        Task Merge(string source, ReportData reportData);
        
    }
}
