using DatabaseManager.Common.Services;
using DatabaseManager.Shared;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace DatabaseManager.Client.Helpers
{
    public class DataFile : IDataFile
    {
        private readonly IHttpService httpService;
        private string url = "api/file";

        public DataFile(IHttpService httpService)
        {
            this.httpService = httpService;
        }

        public async Task<List<string>> GetFiles(string dataType)
        {
            var response = await httpService.Get<List<string>>($"{url}/{dataType}");
            if (!response.Success)
            {
                throw new ApplicationException(await response.GetBody());
            }
            return response.Response;
        }

        public async Task LoadFile(FileParameters fileParameters)
        {
            var response = await httpService.Post(url, fileParameters);
            if (!response.Success)
            {
                throw new ApplicationException(await response.GetBody());
            }
        }
    }
}
