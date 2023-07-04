using DatabaseManager.Services.IndexSqlite.Helpers;
using System.Data;
using static Microsoft.EntityFrameworkCore.DbLoggerCategory.Database;

namespace DatabaseManager.Services.IndexSqlite.Services
{
    public class LASDataAccess : IDataAccess
    {
        private readonly IFileStorageService fileStorageService;
        private ConnectParameters connection;
        private ConnectParameters target;
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

        public Task<IEnumerable<T>> LoadData<T, U>(string storedProcedure, U parameters, string connectionString)
        {
            throw new NotImplementedException();
        }

        public void OpenConnection(ConnectParameters source, ConnectParameters target)
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
    }
}
