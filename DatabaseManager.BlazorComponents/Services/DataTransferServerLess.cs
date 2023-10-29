using DatabaseManager.BlazorComponents.Extensions;
using DatabaseManager.BlazorComponents.Models;
using DatabaseManager.Shared;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace DatabaseManager.BlazorComponents.Services
{
    public class DataTransferServerLess : BaseService, IDataTransfer
    {
        private readonly IHttpClientFactory _clientFactory;
        private readonly SingletonServices _settings;

        public DataTransferServerLess(IHttpClientFactory clientFactory, SingletonServices settings) : base(clientFactory)
        {
            _clientFactory = clientFactory;
            _settings = settings;
        }

        public async Task Copy(TransferParameters transferParameters)
        {
            string url = "";
            if (transferParameters.SourceType == "DataBase")
            {
                url = SD.DataTransferAPIBase.BuildFunctionUrl($"/api/CopyDatabaseObject", $"", SD.DataTransferKey);
            }
            else if(transferParameters.SourceType == "File")
            {
                if (transferParameters.SourceDataType == "Logs")
                {
                    url = SD.DataTransferAPIBase.BuildFunctionUrl($"/api/CopyLASObject", $"", SD.DataTransferKey);
                }
                else
                {
                    url = SD.DataTransferAPIBase.BuildFunctionUrl($"/api/CopyCSVObject", $"", SD.DataTransferKey);
                }
            }
            else
            {
                throw new ApplicationException($"Copy data objects: Source type {transferParameters.SourceType} not supported");
            }
            Console.WriteLine($"Copy: {url}");
            ResponseDto response = await this.SendAsync<ResponseDto>(new ApiRequest()
            {
                ApiType = SD.ApiType.POST,
                AzureStorage = _settings.AzureStorage,
                Url = url,
                Data = transferParameters
            });
            if (!response.IsSuccess)
            {
                throw new ApplicationException(String.Join("Copy data objects:", response.ErrorMessages));
            }
        }

        public async Task CopyRemote(TransferParameters transferParameters)
        {
            string url = SD.DataTransferAPIBase.BuildFunctionUrl($"/api/CopyRemoteObject", $"", SD.DataTransferKey);
            Console.WriteLine($"Copy remote: {url}");
            ResponseDto response = await this.SendAsync<ResponseDto>(new ApiRequest()
            {
                ApiType = SD.ApiType.POST,
                AzureStorage = _settings.AzureStorage,
                Url = url,
                Data = transferParameters
            });
            if (!response.IsSuccess)
            {
                throw new ApplicationException(String.Join("Remote copy data objects:", response.ErrorMessages));
            }
        }

        public async Task DeleteTable(string source, string table)
        {
            string url = SD.DataTransferAPIBase.BuildFunctionUrl($"/api/DeleteObject", $"Name={source}&Table={table}", SD.DataTransferKey);
            Console.WriteLine($"Delete: {url}");
            ResponseDto response = await this.SendAsync<ResponseDto>(new ApiRequest()
            {
                ApiType = SD.ApiType.DELETE,
                AzureStorage = _settings.AzureStorage,
                Url = url
            });
            if (!response.IsSuccess)
            {
                throw new ApplicationException(String.Join("Delete data objects:", response.ErrorMessages));
            }
        }

        public async Task<List<TransferParameters>> GetDataObjects(string source)
        {
            string url = SD.DataTransferAPIBase.BuildFunctionUrl($"/api/GetDataObjects", $"Name={source}", SD.DataTransferKey);
            Console.WriteLine($"Get data objects: {url}");
            ResponseDto response = await this.SendAsync<ResponseDto>(new ApiRequest()
            {
                ApiType = SD.ApiType.GET,
                AzureStorage = _settings.AzureStorage,
                Url = url
            });
            if (!response.IsSuccess)
            {
                throw new ApplicationException(String.Join("Get data objects:", response.ErrorMessages));
            }
            var files = JsonConvert.DeserializeObject<List<string>>(response.Result.ToString());
            List<TransferParameters> transParm = new List<TransferParameters>();
            foreach (var file in files)
            {
                transParm.Add(new TransferParameters { TargetName = source, Table = file });
            }
            return transParm;
        }

        public async Task<List<MessageQueueInfo>> GetQueueMessage()
        {
            string url = SD.DataTransferAPIBase.BuildFunctionUrl($"/api/GetTransferQueueMessage", $"", SD.DataTransferKey);
            Console.WriteLine($"Get queue message: {url}");
            ResponseDto response = await this.SendAsync<ResponseDto>(new ApiRequest()
            {
                ApiType = SD.ApiType.GET,
                AzureStorage = _settings.AzureStorage,
                Url = url
            });
            if (!response.IsSuccess)
            {
                throw new ApplicationException(String.Join("Get queue message:", response.ErrorMessages));
            }
            List<MessageQueueInfo> messages = new List<MessageQueueInfo>();
            string message = response.Result.ToString();
            messages.Add(new MessageQueueInfo { Message = message });
            return messages;
        }
    }
}
