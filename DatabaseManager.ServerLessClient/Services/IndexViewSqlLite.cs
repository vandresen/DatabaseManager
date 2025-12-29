//using DatabaseManager.BlazorComponents.Models;
using DatabaseManager.ServerLessClient.Helpers;
using DatabaseManager.ServerLessClient.Models;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Runtime;

namespace DatabaseManager.ServerLessClient.Services
{
    public class IndexViewSqlLite : BaseService, IIndexView
    {
        private readonly IHttpClientFactory _clientFactory;
        private readonly string _taxonomyShare = "taxonomy";
        private readonly string _indexAPIBase;
        private readonly string _indexKey;
        private readonly string _dataConfigurationKey;
        private readonly string _dataConfigurationApiBase;
        private readonly BlazorSingletonService _settings;

        public IndexViewSqlLite(IHttpClientFactory clientFactory, IConfiguration configuration, BlazorSingletonService settings) : base(clientFactory)
        {
            _clientFactory = clientFactory;
            _indexAPIBase = configuration["ServiceUrls:IndexAPI"]
                ?? throw new InvalidOperationException("Missing ServiceUrls:IndexAPI");
            _indexKey = configuration["ServiceUrls:IndexKey"]
                ?? throw new InvalidOperationException("Missing ServiceUrls:IndexKey");
            _dataConfigurationKey = configuration["ServiceUrls:DataConfigurationKey"]
                ?? throw new InvalidOperationException("Missing ServiceUrls:DataConfigurationKey");
            _dataConfigurationApiBase = configuration["ServiceUrls:DataConfigurationAPI"]
                ?? throw new InvalidOperationException("Missing ServiceUrls:DataConfigurationAPI");
            _settings = settings;
        }

        public async Task CreateProject(string project)
        {
            string url = _indexAPIBase.BuildFunctionUrl("/Project", $"project={project}", _indexKey);
            Console.WriteLine($"GetIndexProject: url = {url}");
            try
            {
                ResponseDto response = await this.SendAsync<ResponseDto>(new ApiRequest()
                {
                    ApiType = SD.ApiType.POST,
                    Url = url
                });
                if (response?.IsSuccess != true || response.Result == null)
                {
                    throw new Exception($"Error creating index: {response.ErrorMessages}");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"CreateProject: Exception - {ex.Message}");
            }
        }

        public async Task DeleteProject(string project)
        {
            string url = _indexAPIBase.BuildFunctionUrl("/Project", $"project={project}", _indexKey);
            Console.WriteLine($"DeleteProject: url = {url}");
            try
            {
                ResponseDto response = await this.SendAsync<ResponseDto>(new ApiRequest()
                {
                    ApiType = SD.ApiType.DELETE,
                    Url = url
                });
                if (response?.IsSuccess != true || response.Result == null)
                {
                    throw new Exception($"Error deleting index: {response.ErrorMessages}");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"DeleteProject: Exception - {ex.Message}");
            }
            
        }

        public async Task<List<DmsIndex>> GetChildren(string source, int id)
        {
            if (string.IsNullOrWhiteSpace(source))
            {
                throw new ArgumentException("Project must be provided", nameof(source));
            }

            List<DmsIndex> children = new List<DmsIndex>();

            string url = _indexAPIBase.BuildFunctionUrl("/DmIndexes", $"project={source}&id={id}", _indexKey);
            Console.WriteLine($"GetIndex: url = {url}");

            try
            {
                ResponseDto response = await this.SendAsync<ResponseDto>(new ApiRequest()
                {
                    ApiType = SD.ApiType.GET,
                    Url = url
                });
                if (response?.IsSuccess != true || response.Result == null)
                {
                    Console.WriteLine($"GetChildren: Request failed or response was null. " +
                                      $"Errors: {string.Join(";", response?.ErrorMessages ?? new List<string>())}");
                    return null;
                }
                children = JsonConvert.DeserializeObject<List<DmsIndex>>(response.Result.ToString());
                return children;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"GetChildren: Exception - {ex.Message}");
                return null;
            }
        }

        public async Task<List<DmsIndex>> GetIndex(string source)
        {
            List<DmsIndex> index = new List<DmsIndex>();
            string url = _indexAPIBase.BuildFunctionUrl("/DmIndexes", $"project={source}", _indexKey);
            Console.WriteLine($"GetIndex: url = {url}");

            try
            {
                ResponseDto response = await this.SendAsync<ResponseDto>(new ApiRequest()
                {
                    ApiType = SD.ApiType.GET,
                    Url = url
                });
                if (response?.IsSuccess != true || response.Result == null)
                {
                    Console.WriteLine($"GetIndex: Request failed or response was null. " +
                                      $"Errors: {string.Join(";", response?.ErrorMessages ?? new List<string>())}");
                    return null;
                }
                index = JsonConvert.DeserializeObject<List<DmsIndex>>(response.Result.ToString());
                return index;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"GetIndex: Exception - {ex.Message}");
                return null;
            }
            
        }

        public async Task<List<IndexFileDefinition>> GetIndexFileDefs(string fileName)
        {
            List<IndexFileDefinition> def = new List<IndexFileDefinition>();
            string url = _dataConfigurationApiBase.BuildFunctionUrl("/api/GetDataConfiguration", $"folder={_taxonomyShare}&name={fileName}", _dataConfigurationKey);
            Console.WriteLine($"GetIndexFileDefs: url = {url}");

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
                def = JsonConvert.DeserializeObject<List<IndexFileDefinition>>(response.Result.ToString());
            }
            catch (Exception ex)
            {
                Console.WriteLine($"GetIndexFileDefs: Exception - {ex.Message}");
            }
            return def;
        }

        public async Task<List<string>> GetIndexProjects()
        {
            List<string> projects = new List<string>();
            string url = _indexAPIBase.BuildFunctionUrl("/Project", $"", _indexKey);
            Console.WriteLine($"GetIndexProject: url = {url}");

            try
            {
                ResponseDto response = await this.SendAsync<ResponseDto>(new ApiRequest()
                {
                    ApiType = SD.ApiType.GET,
                    Url = url
                });
                if (response?.IsSuccess != true || response.Result == null)
                {
                    Console.WriteLine($"GetIndexProjects: Request failed or response was null. " +
                                      $"Errors: {string.Join(";", response?.ErrorMessages ?? new List<string>())}");
                    return null;
                }
                projects = JsonConvert.DeserializeObject<List<string>>(response.Result.ToString());
                return projects;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"GetIndexProjects: Exception - {ex.Message}");
                return null;
            }
            
        }

        public async Task<List<IndexFileData>> GetIndexTaxonomy(string source)
        {
            try
            {
                IndexModel idx = await GetSingleIndexItem(source, 1);
                string jsonData = idx.JsonDataObject;
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

        public async Task<IndexModel> GetSingleIndexItem(string source, int id)
        {
            if (string.IsNullOrWhiteSpace(source))
            {
                throw new ArgumentException("Project must be provided", nameof(source));
            }

            IndexModel idx = new IndexModel();
            string url = _indexAPIBase.BuildFunctionUrl($"/Index/{id}", $"Project={source}", _indexKey);
            Console.WriteLine($"GetSingleIndexItem: url = {url}");

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
                return idx;
            }
        }

        public async Task<List<IndexFileList>> GetTaxonomies()
        {
            List<IndexFileList> result = new List<IndexFileList>();
            string url = _dataConfigurationApiBase.BuildFunctionUrl("/api/GetDataConfiguration", $"folder={_taxonomyShare}", _dataConfigurationKey);
            Console.WriteLine($"GetSingleIndexItem: url = {url}");
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
                    Console.WriteLine($"GetTaxonomies: Request failed or response was null. " +
                                      $"Errors: {string.Join(";", response?.ErrorMessages ?? new List<string>())}");
                    return null;
                }
                List<string> files = JsonConvert.DeserializeObject<List<string>>(response.Result.ToString());
                foreach (string file in files)
                {
                    result.Add(new IndexFileList()
                    {
                        Name = file
                    });
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"GetTaxonomies: Exception - {ex.Message}");
            }
            return result;
        }

        public async Task SaveIndexFileDefs(List<IndexFileDefinition> indexDef, string fileName)
        {
            string url = _dataConfigurationApiBase.BuildFunctionUrl("/api/DataConfiguration", $"folder={_taxonomyShare}&name={fileName}", _dataConfigurationKey);
            Console.WriteLine($"GetSingleIndexItem: url = {url}");
            var jsonIndexDef = JsonConvert.SerializeObject(indexDef);

            try
            {
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
    }
}
