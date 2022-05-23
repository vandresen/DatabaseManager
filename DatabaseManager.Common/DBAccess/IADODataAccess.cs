using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DatabaseManager.Common.DBAccess
{
    public interface IADODataAccess
    {
        Task InsertWithUDT<T>(string storedProcedure, string parameterName, T collection, string connectionString);
    }
}
