using DatabaseManager.Services.DataTransfer.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DatabaseManager.Services.DataTransfer.Helpers
{
    public class Common
    {
        public static async Task<List<string>> GetFiles(ConnectParametersDto source)
        {
            List<string> files = new List<string>();
            try
            {
                if (source.SourceType == "DataBase")
                {
                    //foreach (string tableName in DatabaseTables.Names)
                    //{
                    //    files.Add(tableName);
                    //}
                }
                else if (source.SourceType == "File")
                {
                    if (source.DataType == "Logs")
                    {
                        //files = await _fileStorage.ListFiles(connector.Catalog);
                    }
                    else
                    {
                        //if (string.IsNullOrEmpty(connector.FileName))
                        //{
                        //    Exception error = new Exception($"DataTransfer: Could not get filename for {source}");
                        //    throw error;
                        //}
                        //files.Add(connector.FileName);
                    }
                }
            }
            catch (Exception ex)
            {
                Exception error = new Exception($"DataTransfer: Could not get files for {source}, {ex}");
                throw error;
            }

            return files;
        }
    }
}
