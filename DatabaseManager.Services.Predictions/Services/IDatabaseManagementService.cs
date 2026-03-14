using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DatabaseManager.Services.Predictions.Services
{
    public interface IDatabaseManagementService
    {
        Task<T> GetDataAccessDef<T>();
    }
}
