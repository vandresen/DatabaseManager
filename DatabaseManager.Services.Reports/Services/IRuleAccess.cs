using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DatabaseManager.Services.Reports.Services
{
    public interface IRuleAccess
    {
        Task<T> GetRules<T>(string sourceName);
        Task<T> GetRule<T>(string sourceName, int id);
    }
}
