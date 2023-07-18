using DatabaseManager.Services.Index.Helpers;
using DatabaseManager.Services.Index.Models;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DatabaseManager.Services.Index.Services
{
    public class LASDataAccess : IDataAccess
    {
        private readonly IFileStorageService fileStorageService;
        private ConnectParametersDto connection;
        private ConnectParametersDto target;
        private LASLoader ls;
        private DataTable wellTable;

        public LASDataAccess(IFileStorageService fileStorageService)
        {
            this.fileStorageService = fileStorageService;
        }

        public Task<T> Count<T, U>(string sql, U parameters, string connectionString)
        {
            throw new NotImplementedException();
        }

        public Task DeleteDataSQL<T>(string sql, T parameters, string connectionString)
        {
            throw new NotImplementedException();
        }

        public Task ExecuteSQL(string sql, string connectionString)
        {
            throw new NotImplementedException();
        }

        public async Task<DataTable> GetDataTable(string select, string query, string dataType)
        {
            DataTable dt = new DataTable();
            if (dataType == "WellBore")
            {
                dt = await ls.GetLASWellHeaders(connection, target);
            }
            else if (dataType == "Log")
            {
                if (wellTable == null)
                {
                    wellTable = await ls.GetLASLogHeaders(connection, target);
                }
                string condition = query.Replace("where", "");
                condition = condition.Trim();
                dt = wellTable.Select(condition).CopyToDataTable();
            }
            else
            {

            }
            return dt;
        }

        public Task InsertWithUDT<T>(string storedProcedure, string parameterName, T collection, string connectionString)
        {
            throw new NotImplementedException();
        }

        public Task<IEnumerable<T>> LoadData<T, U>(string storedProcedure, U parameters, string connectionString)
        {
            throw new NotImplementedException();
        }

        public void OpenConnection(ConnectParametersDto source, ConnectParametersDto target)
        {
            this.connection = source;
            this.target = target;
            fileStorageService.SetConnectionString(connection.ConnectionString);
            ls = new LASLoader(fileStorageService);
        }

        public Task<IEnumerable<T>> ReadData<T>(string sql, string connectionString)
        {
            throw new NotImplementedException();
        }

        public Task SaveData<T>(string storedProcedure, T parameters, string connectionString)
        {
            throw new NotImplementedException();
        }

        public Task SaveDataSQL<T>(string sql, T parameters, string connectionString)
        {
            throw new NotImplementedException();
        }

        public void WakeUpDatabase(string connectionString)
        {
            throw new NotImplementedException();
        }
    }
}
