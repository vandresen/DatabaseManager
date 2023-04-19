using DatabaseManager.Services.DataTransfer.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DatabaseManager.Services.DataTransfer.Services
{
    public class CSVTransfer : IDataTransfer
    {
        public void CopyData(TransferParameters transParms)
        {
            throw new NotImplementedException();
        }

        public void DeleteData(ConnectParametersDto source, string table)
        {
            throw new NotImplementedException();
        }

        public async Task<List<string>> GetContainers(ConnectParametersDto source)
        {
            List<string> containers = new List<string>();
            if (string.IsNullOrEmpty(source.FileName))
            {
                Exception error = new Exception($"DataTransfer: Could not get filename for {source.SourceName}");
                throw error;
            }
            containers.Add(source.FileName);
            return containers;
        }
    }
}
