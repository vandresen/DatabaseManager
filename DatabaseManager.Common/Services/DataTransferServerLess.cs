using DatabaseManager.Common.Extensions;
using DatabaseManager.Shared;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace DatabaseManager.Common.Services
{
    public class DataTransferServerLess : IDataTransfer
    {
        private readonly IHttpService httpService;
        private readonly string baseUrl;
        private readonly string apiKey;

        public DataTransferServerLess(IHttpService httpService, SingletonServices settings)
        {
            this.httpService = httpService;
            baseUrl = settings.BaseUrl;
            apiKey = settings.ApiKey;
        }

        public async Task Copy(TransferParameters transferParameters)
        {
            string url = baseUrl.BuildFunctionUrl("TransferData", $"", apiKey);
            var response = await httpService.Post(url, transferParameters);
            if (!response.Success)
            {
                throw new ApplicationException(await response.GetBody());
            }
        }

        public async Task CopyRemote(TransferParameters transferParameters)
        {
            string url = baseUrl.BuildFunctionUrl("TransferRemote", $"", apiKey);
            Console.WriteLine($"Copy: {url}");
            var response = await httpService.Post(url, transferParameters);
            if (!response.Success)
            {
                throw new ApplicationException(await response.GetBody());
            }
        }

        public async Task DeleteTable(string source, string table)
        {
            string url = baseUrl.BuildFunctionUrl("DeleteTable", $"name={source}&table={table}", apiKey);
            var response = await httpService.Delete(url);
            if (!response.Success)
            {
                throw new ApplicationException(await response.GetBody());
            }
        }

        public async Task<List<TransferParameters>> GetDataObjects(string source)
        {
            string url = baseUrl.BuildFunctionUrl("GetDataObjects", $"name={source}", apiKey);
            var response = await httpService.Get<List<TransferParameters>>(url);
            if (!response.Success)
            {
                throw new ApplicationException(await response.GetBody());
            }
            return response.Response;
        }

        public async Task<List<MessageQueueInfo>> GetQueueMessage()
        {
            string url = baseUrl.BuildFunctionUrl("GetTransferQueueMessage", $"", apiKey);
            Console.WriteLine($"Copy: {url}");
            var response = await httpService.Get<List<MessageQueueInfo>>($"{url}");
            if (!response.Success)
            {
                throw new ApplicationException(await response.GetBody());
            }
            return response.Response;
        }
    }
}
