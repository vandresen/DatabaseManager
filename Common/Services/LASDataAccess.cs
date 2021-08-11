using DatabaseManager.Common.Helpers;
using DatabaseManager.Shared;
using System;
using System.Collections.Generic;
using System.Data;
using System.Text;
using System.Threading.Tasks;

namespace DatabaseManager.Common.Services
{
    public class LASDataAccess : IDataAccess
    {
        private readonly IFileStorageServiceCommon fileStorageService;
        private LASLoader ls;
        private ConnectParameters connection;
        private ConnectParameters target;
        private DataTable wellTable;

        public LASDataAccess(IFileStorageServiceCommon fileStorageService)
        {
            this.fileStorageService = fileStorageService;
        }

        public void OpenConnection(ConnectParameters source, ConnectParameters target)
        {
            this.connection = source;
            this.target = target;
            fileStorageService.SetConnectionString(connection.ConnectionString);
            ls = new LASLoader(fileStorageService);
            Console.WriteLine("Open LAS connection");
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
                Console.WriteLine($"Total logs {wellTable.Rows.Count}");
                string condition = query.Replace("where", "");
                condition = condition.Trim();
                dt = wellTable.Select(condition).CopyToDataTable();
                Console.WriteLine($"Filtered logs for {query} {dt.Rows.Count}");
            }
            else
            {

            }
            return dt;
        }

        public void CloseConnection()
        {
            Console.WriteLine("Close LAS connection");
        }

    }
}
