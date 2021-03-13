using DatabaseManager.Server.Helpers;
using DatabaseManager.Shared;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Threading.Tasks;

namespace DatabaseManager.Server.Services
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

        public void OpenConnection(ConnectParameters source, ConnectParameters target)
        {
            this.connection = source;
            this.target = target;
            fileStorageService.SetConnectionString(connection.ConnectionString);
            csv = new CSVLoader(fileStorageService);
        }
    }
}
