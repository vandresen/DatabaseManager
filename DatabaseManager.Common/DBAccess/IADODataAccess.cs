using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DatabaseManager.Common.DBAccess
{
    public interface IADODataAccess
    {
        Task InsertWithUDT<T>(string storedProcedure, string parameterName, T collection, string connectionString);
        DataTable GetDataTable(string sql, string connectionString);
        void Delete(string table, string connectionString);
        void ExecuteSQL(string sql, string connectionString); 
    }
}
