using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DatabaseManager.Services.DataOps.Services
{
    public interface IRuleAccess
    {
        Task<T> GetRules<T>(string sourceName);
    }
}
