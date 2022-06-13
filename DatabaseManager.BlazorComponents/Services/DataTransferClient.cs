using DatabaseManager.Shared;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DatabaseManager.BlazorComponents.Services
{
    public class DataTransferClient : IDataTransfer
    {
        private readonly IHttpService httpService;
        private string url = "api/datatransfer";

        public DataTransferClient(IHttpService httpService)
        {
            this.httpService = httpService;
        }

        public async Task<List<MessageQueueInfo>> GetQueueMessage()
        {
            var response = await httpService.Get<List<MessageQueueInfo>>($"{url}");
            if (!response.Success)
            {
                throw new ApplicationException(await response.GetBody());
            }
            return response.Response;
        }

        public async Task<List<TransferParameters>> GetDataObjects(string source)
        {
            var response = await httpService.Get<List<TransferParameters>>($"{url}/{source}");
            if (!response.Success)
            {
                throw new ApplicationException(await response.GetBody());
            }
            return response.Response;
        }

        public async Task Copy(TransferParameters transferParameters)
        {
            var response = await httpService.Post(url, transferParameters);
            if (!response.Success)
            {
                throw new ApplicationException(await response.GetBody());
            }
        }

        public async Task CopyRemote(TransferParameters transferParameters)
        {
            var response = await httpService.Post($"{url}/remote", transferParameters);
            if (!response.Success)
            {
                throw new ApplicationException(await response.GetBody());
            }
        }

        public async Task DeleteTable(string source, string table)
        {
            var response = await httpService.Delete($"{url}/{source}/{table}");
            if (!response.Success)
            {
                throw new ApplicationException(await response.GetBody());
            }
        }
    }
}
