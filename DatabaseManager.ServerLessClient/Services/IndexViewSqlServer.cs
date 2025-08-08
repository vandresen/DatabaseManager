using DatabaseManager.ServerLessClient.Helpers;
using DatabaseManager.ServerLessClient.Models;
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;

namespace DatabaseManager.ServerLessClient.Services
{
    public class IndexViewSqlServer : BaseService, IIndexView
    {
        private readonly IHttpClientFactory _clientFactory;
        private readonly BlazorSingletonService _settings;
        private readonly string _indexAPIBase;
        private readonly string _indexKey;
        private readonly string _dataConfigurationAPIBase;
        private readonly string _dataConfigurationKey;
        private readonly string _taxonomyShare = "taxonomy";

        public IndexViewSqlServer(IHttpClientFactory clientFactory,
            IConfiguration configuration, BlazorSingletonService settings) : base(clientFactory)
        {
            _clientFactory = clientFactory;
            _settings = settings;
            _indexAPIBase = configuration["ServiceUrls:IndexAPI"]
                ?? throw new InvalidOperationException("Missing ServiceUrls:IndexAPI");
            _indexKey = configuration["ServiceUrls:IndexKey"]
                ?? throw new InvalidOperationException("Missing ServiceUrls:IndexKey");
            _dataConfigurationAPIBase = configuration["ServiceUrls:DataConfigurationAPI"]
                ?? throw new InvalidOperationException("Missing ServiceUrls:DataConfigurationAPI");
            _dataConfigurationKey = configuration["ServiceUrls:DataConfigurationKey"]
                ?? throw new InvalidOperationException("Missing ServiceUrls:DataConfigurationKey");

        }

        public async Task<List<DmsIndex>> GetChildren(string source, int id)
        {
            List<DmsIndex> children = new List<DmsIndex>();
            if (string.IsNullOrWhiteSpace(source))
            {
                throw new ArgumentException("Source must be provided", nameof(source));
            }
            string url = _indexAPIBase.BuildFunctionUrl("/api/DmIndex", $"name={source}&id={id}", _indexKey);
            Console.WriteLine($"GetDmIndexesAsync: url = {url}");

            try
            {
                ResponseDto response = await this.SendAsync<ResponseDto>(new ApiRequest()
                {
                    ApiType = SD.ApiType.GET,
                    Url = url
                });

                if (response is null)
                {
                    Console.WriteLine("GetChildren: response is null");
                    return children;
                }

                if (response.IsSuccess && response.Result is not null)
                {
                    children = JsonConvert.DeserializeObject<List<DmsIndex>>(response.Result.ToString());
                }
                else
                {
                    Console.WriteLine($"GetChildren failed: {string.Join("; ", response.ErrorMessages ?? new List<string> { "Unknown error" })}");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Exception in GetChildren: {ex.Message}");
            }
            
            return children;
        }

        public async Task<List<DmsIndex>> GetIndex(string source)
        {
            List<DmsIndex> index = new List<DmsIndex>();
            string url = _indexAPIBase.BuildFunctionUrl("/api/DmIndexes", $"name={source}", _indexKey);
            Console.WriteLine($"GetIndex: url = {url}");
            ResponseDto response = await this.SendAsync<ResponseDto>(new ApiRequest()
            {
                ApiType = SD.ApiType.GET,
                Url = url
            });
            if (response.IsSuccess)
            {
                index = JsonConvert.DeserializeObject<List<DmsIndex>>(response.Result.ToString());
            }
            else
            {
                Console.WriteLine($"GetIndex error: {response.ErrorMessages}");
            }
            return index;
        }

        public async Task<List<IndexFileData>> GetIndexTaxonomy(string source)
        {
            if (string.IsNullOrWhiteSpace(source))
            {
                throw new ArgumentException("Source must be provided", nameof(source));
            }

            string url = _indexAPIBase.BuildFunctionUrl("/api/DmIndexes", $"Name={source}&Node=/&Level=0", _indexKey);
            Console.WriteLine($"GetDmIndexesAsync: url = {url}");

            try
            {
                ResponseDto response = await this.SendAsync<ResponseDto>(new ApiRequest()
                {
                    ApiType = SD.ApiType.GET,
                    Url = url
                });

                if (response == null || !response.IsSuccess || response.Result == null)
                {
                    Console.WriteLine("GetIndexTaxonomy: Failed to get a successful response");
                    return new List<IndexFileData>();
                }

                List<DmsIndex> idxList = JsonConvert.DeserializeObject<List<DmsIndex>>(response.Result.ToString());
                if (idxList == null || idxList.Count == 0)
                {
                    Console.WriteLine("GetIndexTaxonomy: No index data returned");
                    return new List<IndexFileData>();
                }

                string jsonData = idxList[0].JsonData;
                if (string.IsNullOrWhiteSpace(jsonData))
                {
                    Console.WriteLine("GetIndexTaxonomy: JsonData is empty");
                    return new List<IndexFileData>();
                }

                var rootJson = JsonConvert.DeserializeObject<IndexRootJson>(jsonData);
                if (rootJson == null || string.IsNullOrWhiteSpace(rootJson.Taxonomy))
                {
                    Console.WriteLine("GetIndexTaxonomy: Taxonomy data is missing");
                    return new List<IndexFileData>();
                }

                JArray taxonomyArray;
                try
                {
                    taxonomyArray = JArray.Parse(rootJson.Taxonomy);
                }
                catch (JsonReaderException jex)
                {
                    Console.WriteLine($"GetIndexTaxonomy: Failed to parse taxonomy JSON - {jex.Message}");
                    return new List<IndexFileData>();
                }

                List<IndexFileData> idxData = new List<IndexFileData>();
                foreach (JToken level in taxonomyArray)
                {
                    idxData.Add(ProcessJTokens(level));
                    idxData = ProcessIndexArray(taxonomyArray, level, idxData);
                }
                return idxData;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"GetIndexTaxonomy: Exception - {ex.Message}");
                return new List<IndexFileData>();
            }
        }

        public async Task<List<IndexFileList>> GetTaxonomies()
        {
            List<IndexFileList> result = new();
            string url = _dataConfigurationAPIBase.BuildFunctionUrl("/api/GetDataConfiguration", $"folder={_taxonomyShare}", _dataConfigurationKey);
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

        public async Task<IndexModel> GetSingleIndexItem(string source, int id)
        {
            if (string.IsNullOrWhiteSpace(source))
            {
                throw new ArgumentException("Source must be provided", nameof(source));
            }

            IndexModel idx = new IndexModel();
            string url = _indexAPIBase.BuildFunctionUrl($"/api/Indexes/{id}", $"Name={source}", _indexKey);
            Console.WriteLine($"GetDmIndexesAsync: url = {url}");

            try
            {
                ResponseDto response = await this.SendAsync<ResponseDto>(new ApiRequest()
                {
                    ApiType = SD.ApiType.GET,
                    Url = url
                });
                if (response?.IsSuccess != true || response.Result == null)
                {
                    Console.WriteLine($"GetSingleIndexItem: Request failed or response was null. " +
                                      $"Errors: {string.Join(";", response?.ErrorMessages ?? new List<string>())}");
                    return null;
                }
                return JsonConvert.DeserializeObject<IndexModel>(response.Result.ToString());
            }
            catch (Exception ex)
            {
                Console.WriteLine($"GetSingleIndexItem: Exception - {ex.Message}");
                return null;
            }
        }

        public async Task<List<IndexFileDefinition>> GetIndexFileDefs(string fileName)
        {
            if (string.IsNullOrWhiteSpace(fileName))
            {
                throw new ArgumentException("File name must be provided", nameof(fileName));
            }

            List<IndexFileDefinition> def = new List<IndexFileDefinition>();
            string url = _dataConfigurationAPIBase.BuildFunctionUrl("/api/GetDataConfiguration", $"folder={_taxonomyShare}&name={fileName}", _dataConfigurationKey);
            Console.WriteLine(url);

            try
            {
                ResponseDto response = await this.SendAsync<ResponseDto>(new ApiRequest()
                {
                    ApiType = SD.ApiType.GET,
                    AzureStorage = _settings.AzureStorage,
                    Url = url
                });
                if (response?.IsSuccess != true || response.Result == null)
                {
                    Console.WriteLine($"GetIndexFileDefs: Request failed or response was null. " +
                                      $"Errors: {string.Join(";", response?.ErrorMessages ?? new List<string>())}");
                    return null;
                }
                return JsonConvert.DeserializeObject<List<IndexFileDefinition>>(response.Result.ToString());
            }
            catch (Exception ex)
            {
                Console.WriteLine($"GetIndexFileDefs: Exception - {ex.Message}");
                return null;
            }
            
        }

        public async Task SaveIndexFileDefs(List<IndexFileDefinition> indexDef, string fileName)
        {
            if (string.IsNullOrWhiteSpace(fileName))
            {
                throw new ArgumentException("File name must be provided", nameof(fileName));
            }

            try
            {
                string url = _dataConfigurationAPIBase.BuildFunctionUrl("/api/DataConfiguration", $"folder={_taxonomyShare}&name={fileName}", _dataConfigurationKey);
                Console.WriteLine(url);
                ResponseDto response = await this.SendAsync<ResponseDto>(new ApiRequest()
                {
                    ApiType = SD.ApiType.POST,
                    AzureStorage = _settings.AzureStorage,
                    Url = url,
                    Data = indexDef
                });
                if (response?.IsSuccess != true || response.Result == null)
                {
                    Console.WriteLine($"SaveIndexFileDefs: Request failed or response was null. " +
                                      $"Errors: {string.Join(";", response?.ErrorMessages ?? new List<string>())}");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"SaveIndexFileDefs: Exception - {ex.Message}");
            }
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
