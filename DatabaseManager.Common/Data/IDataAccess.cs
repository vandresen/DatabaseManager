using DatabaseManager.Shared;
using System;
using System.Collections.Generic;
using System.Data;
using System.Text;
using System.Threading.Tasks;

namespace DatabaseManager.Common.Data
{
    public interface IDataAccess
    {
        void CloseConnection();
        Task<DataTable> GetDataTable(string select, string query, string dataType);
        void OpenConnection(ConnectParameters source, ConnectParameters target);
    }
}
