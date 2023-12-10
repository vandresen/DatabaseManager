using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DatabaseManager.Services.DataQC.Services
{
    public interface IDataAccess
    {
        DataTable GetDataTable(string sql, string connectionString);
    }
}
