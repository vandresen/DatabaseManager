using DatabaseManager.Services.IndexSqlite.Helpers;
using System.Data;

namespace DatabaseManager.Services.IndexSqlite.Services
{
    public class CSVDataAccess : IDataAccess
    {
        private readonly IFileStorageService fileStorageService;
        private ConnectParameters connection;
        private ConnectParameters target;
        private CSVLoader csv;

        public CSVDataAccess(IFileStorageService fileStorageService)
        {
            this.fileStorageService = fileStorageService;
        }
        public void CloseConnection()
        {
            Console.WriteLine("Close CSV connection");
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
            DataTable dt = await csv.GetCSVTable(connection, target, dataType);
            if (!string.IsNullOrEmpty(query))
            {
                string condition = query.Replace("where", "");
                condition = condition.Trim();
                if (dt.Rows.Count > 0) dt = dt.Select(condition).CopyToDataTable();
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
            csv = new CSVLoader(fileStorageService);
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
