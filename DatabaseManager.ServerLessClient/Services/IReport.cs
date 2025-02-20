using DatabaseManager.ServerLessClient.Models;
using DatabaseManager.Shared;

namespace DatabaseManager.ServerLessClient.Services
{
    public interface IReport
    {
        Task<List<DatabaseManager.ServerLessClient.Models.QcResult>> GetResults(string source);
        Task<List<DatabaseManager.ServerLessClient.Models.TableSchema>> GetAttributeInfo(string source, string dataType);
        //Task<List<DmsIndex>> GetResult(string source, int id);
        //Task<DataQCParameters> ProcessQCRule(DataQCParameters qcParams);
        //Task CloseQC(string source, List<RuleFailures> failures);
        //Task ClearQCFlags(string source);
    }
}
