using DatabaseManager.BlazorComponents.Extensions;
using DatabaseManager.BlazorComponents.Models;
using DatabaseManager.BlazorComponents.Pages.Settings;
using DatabaseManager.Shared;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace DatabaseManager.BlazorComponents.Services
{
    public class IndexViewServerless : BaseService, IIndexView
    {
        private readonly IHttpClientFactory _clientFactory;
        private readonly string taxonomyShare = "taxonomy";
        private readonly SingletonServices _settings;
        private string url;

        public IndexViewServerless(IHttpClientFactory clientFactory, SingletonServices settings) : base(clientFactory)
        {
            _settings = settings;
        }

        public async Task<List<DmsIndex>> GetChildren(string source, int id)
        {
            List<DmsIndex> children = new List<DmsIndex>();
            string url = _settings.IndexAPIBase.BuildFunctionUrl("/api/DmIndex", $"name={source}&id={id}", _settings.IndexKey);
            Console.WriteLine($"GetDmIndexesAsync: url = {url}");
            ResponseDto response = await this.SendAsync<ResponseDto>(new ApiRequest()
            {
                ApiType = SD.ApiType.GET,
                Url = url
            });
            children = JsonConvert.DeserializeObject<List<DmsIndex>>(response.Result.ToString());
            return children;
        }

        public async Task<List<DmsIndex>> GetIndex(string source)
        {
            List<DmsIndex> index = new List<DmsIndex>();
            string url = _settings.IndexAPIBase.BuildFunctionUrl("/api/DmIndexes", $"name={source}", _settings.IndexKey);
            Console.WriteLine($"GetIndex: url = {url}");
            ResponseDto response = await this.SendAsync<ResponseDto>(new ApiRequest()
            {
                ApiType = SD.ApiType.GET,
                Url = url
            });
            if(response.IsSuccess) 
            {
                index = JsonConvert.DeserializeObject<List<DmsIndex>>(response.Result.ToString());
            }
            else
            {
                Console.WriteLine($"GetIndex error: {response.ErrorMessages}");
            }
            return index;
        }

        public async Task<List<IndexFileDefinition>> GetIndexFileDefs(string fileName)
        {
            List<IndexFileDefinition> def = new List<IndexFileDefinition>();
            if (string.IsNullOrEmpty(_settings.DataConfigurationAPIBase)) url = $"api/DataConfiguration?folder={taxonomyShare}&name={fileName}";
            else url = _settings.DataConfigurationAPIBase.BuildFunctionUrl("/api/GetDataConfiguration", $"folder={taxonomyShare}&name={fileName}", _settings.DataConfigurationKey);
            Console.WriteLine(url);
            ResponseDto response = await this.SendAsync<ResponseDto>(new ApiRequest()
            {
                ApiType = SD.ApiType.GET,
                AzureStorage = _settings.AzureStorage,
                Url = url
            });
            def = JsonConvert.DeserializeObject<List<IndexFileDefinition>>(response.Result.ToString());
            return def;
        }

        public async Task<List<IndexFileData>> GetIndexTaxonomy(string source)
        {
            string url = _settings.IndexAPIBase.BuildFunctionUrl("/api/DmIndexes", $"Name={source}&Node=/&Level=0", _settings.IndexKey);
            Console.WriteLine($"GetDmIndexesAsync: url = {url}");
            ResponseDto response =  await this.SendAsync<ResponseDto>(new ApiRequest()
            {
                ApiType = SD.ApiType.GET,
                Url = url
            });
            List<DmsIndex> idx = JsonConvert.DeserializeObject<List<DmsIndex>>(response.Result.ToString());
            string jsonData = idx[0].JsonData;
            IndexRootJson rootJson = JsonConvert.DeserializeObject<IndexRootJson>(idx[0].JsonData);
            JArray jArray = new JArray();
            JArray JsonIndexArray = JArray.Parse(rootJson.Taxonomy);
            List<IndexFileData> idxData = new List<IndexFileData>();
            foreach (JToken level in JsonIndexArray)
            {
                idxData.Add(ProcessJTokens(level));
                idxData = ProcessIndexArray(JsonIndexArray, level, idxData);
            }
            return idxData;
        }

        public async Task<IndexModel> GetSingleIndexItem(string source, int id)
        {
            IndexModel idx = new IndexModel();
            string url = _settings.IndexAPIBase.BuildFunctionUrl($"/api/Indexes/{id}", $"Name={source}", _settings.IndexKey);
            Console.WriteLine($"GetDmIndexesAsync: url = {url}");
            ResponseDto response = await this.SendAsync<ResponseDto>(new ApiRequest()
            {
                ApiType = SD.ApiType.GET,
                Url = url
            });
            string json = response.Result.ToString();
            idx = JsonConvert.DeserializeObject<IndexModel>(json);
            return idx;
        }

        public void InitSettings(SingletonServices settings)
        {
        }

        public async Task SaveIndexFileDefs(List<IndexFileDefinition> indexDef, string fileName)
        {
            if (string.IsNullOrEmpty(SD.DataConfigurationAPIBase)) url = $"api/DataConfiguration?folder={taxonomyShare}&name={fileName}";
            else url = SD.DataConfigurationAPIBase.BuildFunctionUrl("/api/DataConfiguration", $"folder={taxonomyShare}&name={fileName}", SD.DataConfigurationKey);
            Console.WriteLine(url);
            ResponseDto response = await this.SendAsync<ResponseDto>(new ApiRequest()
            {
                ApiType = SD.ApiType.POST,
                AzureStorage = _settings.AzureStorage,
                Url = url,
                Data = indexDef
            });
        }

        private static IndexFileData ProcessJTokens(JToken token)
        {
            IndexFileData idxDataObject = new IndexFileData();
            idxDataObject.DataName = (string)token["DataName"];
            idxDataObject.NameAttribute = token["NameAttribute"]?.ToString();
            idxDataObject.LatitudeAttribute = token["LatitudeAttribute"]?.ToString();
            idxDataObject.LongitudeAttribute = token["LongitudeAttribute"]?.ToString();
            idxDataObject.ParentKey = token["ParentKey"]?.ToString();
            if (token["UseParentLocation"] != null) idxDataObject.UseParentLocation = (Boolean)token["UseParentLocation"];
            if (token["Arrays"] != null)
            {
                idxDataObject.Arrays = token["Arrays"];
            }
            return idxDataObject;
        }

        private List<IndexFileData> ProcessIndexArray(JArray JsonIndexArray, JToken parent, List<IndexFileData> idxData)
        {
            List<IndexFileData> result = idxData;
            if (parent["DataObjects"] != null)
            {
                foreach (JToken level in parent["DataObjects"])
                {
                    result.Add(ProcessJTokens(level));
                    result = ProcessIndexArray(JsonIndexArray, level, result);
                }
            }
            return result;
        }

        public Task<IndexModel> GetIndexroot(string source)
        {
            throw new NotImplementedException();
        }

        public Task<List<string>> GetIndexProjects()
        {
            throw new NotImplementedException();
        }

        public Task CreateProject(string project)
        {
            throw new NotImplementedException();
        }

        public Task DeleteProject(string project)
        {
            throw new NotImplementedException();
        }
    }
}
