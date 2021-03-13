using DatabaseManager.Shared;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Threading.Tasks;

namespace DatabaseManager.Server.Services
{
    public interface IDataAccess
    {
        void CloseConnection();
        Task<DataTable> GetDataTable(string select, string query, string dataType);
        void OpenConnection(ConnectParameters source, ConnectParameters target);
    }
}
