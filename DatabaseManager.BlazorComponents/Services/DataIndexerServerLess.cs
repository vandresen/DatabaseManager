using DatabaseManager.BlazorComponents.Extensions;
using DatabaseManager.BlazorComponents.Models;
using DatabaseManager.BlazorComponents.Pages.Rules;
using DatabaseManager.Shared;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

namespace DatabaseManager.BlazorComponents.Services
{
    public class DataIndexerServerLess : BaseService, IDataIndexer
    {
        private readonly IHttpClientFactory _clientFactory;
        private readonly SingletonServices _settings;
        private string url;
        private readonly string taxonomyShare = "taxonomy";

        public DataIndexerServerLess(IHttpClientFactory clientFactory, SingletonServices settings) : base(clientFactory)
        {
            _settings = settings;
            _clientFactory = clientFactory;
        }

        public async Task Create(CreateIndexParameters iParameters)
        {
            if (SD.Sqlite == true) url = SD.IndexAPIBase.BuildFunctionUrl("/BuildIndex", $"", SD.IndexKey);
            else if (string.IsNullOrEmpty(SD.IndexAPIBase)) url = $"api/createindex";
            else url = SD.IndexAPIBase.BuildFunctionUrl("/api/BuildIndex", $"", SD.IndexKey);
            Console.WriteLine(url);
            ResponseDto response = await this.SendAsync<ResponseDto>(new ApiRequest()
            {
                ApiType = SD.ApiType.POST,
                AzureStorage = _settings.AzureStorage,
                Data = iParameters,
                Url = url
            });
            if (!response.IsSuccess)
            {
                throw new Exception($"Error creating index: {response.ErrorMessages}");
            }
        }

        public async Task<List<IndexFileList>> GetTaxonomies()
        {
            List<IndexFileList> result = new ();
            if (string.IsNullOrEmpty(_settings.DataConfigurationAPIBase)) url = $"api/DataConfiguration?folder={taxonomyShare}";
            else url = _settings.DataConfigurationAPIBase.BuildFunctionUrl("/api/GetDataConfiguration", $"folder={taxonomyShare}", _settings.DataConfigurationKey);
            Console.WriteLine(url);
            ResponseDto response = await this.SendAsync<ResponseDto>(new ApiRequest()
            {
                ApiType = SD.ApiType.GET,
                AzureStorage = _settings.AzureStorage,
                Url = url
            });
            List<string> files = JsonConvert.DeserializeObject<List<string>>(response.Result.ToString());
            foreach (string file in files)
            {
                result.Add(new IndexFileList()
                {
                    Name = file
                });
            }
            return result;
        }

        public Task<CreateIndexParameters> GetTaxonomy(string Name)
        {
            throw new NotImplementedException();
        }
    }
}
