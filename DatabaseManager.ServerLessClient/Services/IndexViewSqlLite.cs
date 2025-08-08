using DatabaseManager.ServerLessClient.Helpers;
using DatabaseManager.ServerLessClient.Models;
using Newtonsoft.Json;
using System;

namespace DatabaseManager.ServerLessClient.Services
{
    public class IndexViewSqlLite : BaseService, IIndexView
    {
        private readonly IHttpClientFactory _clientFactory;
        private readonly string _taxonomyShare = "taxonomy";
        private readonly string _indexAPIBase;
        private readonly string _indexKey;

        public IndexViewSqlLite(IHttpClientFactory clientFactory, IConfiguration configuration) : base(clientFactory)
        {
            _clientFactory = clientFactory;
            _indexAPIBase = configuration["ServiceUrls:IndexAPI"]
                ?? throw new InvalidOperationException("Missing ServiceUrls:IndexAPI");
            _indexKey = configuration["ServiceUrls:IndexKey"]
                ?? throw new InvalidOperationException("Missing ServiceUrls:IndexKey");
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

        public Task<List<DmsIndex>> GetChildren(string source, int id)
        {
            throw new NotImplementedException();
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

        public Task<List<IndexFileDefinition>> GetIndexFileDefs(string fileName)
        {
            throw new NotImplementedException();
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

        public Task<List<IndexFileData>> GetIndexTaxonomy(string source)
        {
            throw new NotImplementedException();
        }

        public Task<IndexModel> GetSingleIndexItem(string source, int id)
        {
            throw new NotImplementedException();
        }

        public Task<List<IndexFileList>> GetTaxonomies()
        {
            throw new NotImplementedException();
        }

        public Task SaveIndexFileDefs(List<IndexFileDefinition> indexDef, string fileName)
        {
            throw new NotImplementedException();
        }
    }
}
