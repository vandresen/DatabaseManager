using DatabaseManager.Services.DataTransfer.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DatabaseManager.Services.DataTransfer.Services
{
    public class DatabaseTransfer : IDataTransfer
    {
        public void CopyData(TransferParameters transParms)
        {
            throw new NotImplementedException();
        }

        public async Task<List<string>> GetContainers(ConnectParametersDto source)
        {
            List<string> containers = new List<string>();
            foreach (string tableName in DatabaseTables.Names)
            {
                containers.Add(tableName);
            }
            return containers;
        }
    }
}
