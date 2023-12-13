using DatabaseManager.Services.DataQC.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DatabaseManager.Services.DataQC.Services
{
    public interface IDataQc
    {
        Task<List<int>> QualityCheckDataType(DataQCParameters parms, List<IndexDto> indexes, RuleModelDto rule);
    }
}
