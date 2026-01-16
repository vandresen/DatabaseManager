using DatabaseManager.Services.DataQuality.Models;

namespace DatabaseManager.Services.DataQuality.Services
{
    public interface IDataQc
    {
        Task<DataQcResult> QualityCheckDataAsync(DataQCParameters parms, List<IndexDto> indexes, RuleModelDto rule);
    }
}
