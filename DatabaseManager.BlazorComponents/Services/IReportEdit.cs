using DatabaseManager.Shared;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DatabaseManager.BlazorComponents.Services
{
    public interface IReportEdit
    {
        Task Update(string source, ReportData reportData);
        Task Insert(string source, ReportData reportData);
        Task<AttributeInfo> GetAttributeInfo(string source, string dataType);
        Task Delete(string source, int id);
    }
}
