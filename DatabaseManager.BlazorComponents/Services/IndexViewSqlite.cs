using DatabaseManager.BlazorComponents.Extensions;
using DatabaseManager.BlazorComponents.Models;
using DatabaseManager.Shared;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

namespace DatabaseManager.BlazorComponents.Services
{
    public class IndexViewSqlite : BaseService, IIndexView
    {
        private readonly IHttpClientFactory _clientFactory;

        public IndexViewSqlite(IHttpClientFactory clientFactory) : base(clientFactory)
        {
            _clientFactory = clientFactory;
        }

        public async Task<List<DmsIndex>> GetChildren(string project, int id)
        {
            List<DmsIndex> children = new List<DmsIndex>();
            string url = SD.IndexAPIBase.BuildFunctionUrl("/DmIndexes", $"id={id}&project={project}", SD.IndexKey);
            Console.WriteLine($"GetChildren: url = {url}");
            ResponseDto response = await this.SendAsync<ResponseDto>(new ApiRequest()
            {
                ApiType = SD.ApiType.GET,
                Url = url
            });
            children = JsonConvert.DeserializeObject<List<DmsIndex>>(response.Result.ToString());
            return children;
        }

        public async Task<List<DmsIndex>> GetIndex(string project)
        {
            List<DmsIndex> index = new List<DmsIndex>();
            string url = SD.IndexAPIBase.BuildFunctionUrl("/DmIndexes", $"project={project}", SD.IndexKey);
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

        public Task<List<IndexFileDefinition>> GetIndexFileDefs(string fileName)
        {
            throw new NotImplementedException();
        }

        public async Task<List<IndexFileData>> GetIndexTaxonomy(string project)
        {
            List<IndexFileData> idxData = new List<IndexFileData>();
            string url = SD.IndexAPIBase.BuildFunctionUrl($"/Index/1", $"project={project}", SD.IndexKey);
            Console.WriteLine($"GetDmIndexesAsync: url = {url}");
            ResponseDto response = await this.SendAsync<ResponseDto>(new ApiRequest()
            {
                ApiType = SD.ApiType.GET,
                Url = url
            });
            IndexModel idx = JsonConvert.DeserializeObject<IndexModel>(response.Result.ToString());
            string jsonData = idx.JsonDataObject;
            IndexRootJson rootJson = JsonConvert.DeserializeObject<IndexRootJson>(jsonData);
            JArray jArray = new JArray();
            JArray JsonIndexArray = JArray.Parse(rootJson.Taxonomy);

            foreach (JToken level in JsonIndexArray)
            {
                idxData.Add(ProcessJTokens(level));
                idxData = ProcessIndexArray(JsonIndexArray, level, idxData);
            }
            return idxData;
        }

        public Task<IndexModel> GetSingleIndexItem(string source, int id)
        {
            throw new NotImplementedException();
        }

        public void InitSettings(SingletonServices settings)
        {
        }

        public Task SaveIndexFileDefs(List<IndexFileDefinition> indexDef, string fileName)
        {
            throw new NotImplementedException();
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

        public async Task<List<string>> GetIndexProjects()
        {
            List<string> projects = new List<string>();
            string url = SD.IndexAPIBase.BuildFunctionUrl("/Project", $"", SD.IndexKey);
            Console.WriteLine($"GetIndexProject: url = {url}");
            ResponseDto response = await this.SendAsync<ResponseDto>(new ApiRequest()
            {
                ApiType = SD.ApiType.GET,
                Url = url
            });
            projects = JsonConvert.DeserializeObject<List<string>>(response.Result.ToString());
            return projects;
        }

        public async Task CreateProject(string project)
        {
            string url = SD.IndexAPIBase.BuildFunctionUrl("/Project", $"project={project}", SD.IndexKey);
            Console.WriteLine($"GetIndexProject: url = {url}");
            ResponseDto response = await this.SendAsync<ResponseDto>(new ApiRequest()
            {
                ApiType = SD.ApiType.POST,
                Url = url
            });
            if (!response.IsSuccess)
            {
                throw new Exception($"Error creating index: {response.ErrorMessages}");
            }
        }

        public async Task DeleteProject(string project)
        {
            string url = SD.IndexAPIBase.BuildFunctionUrl("/Project", $"project={project}", SD.IndexKey);
            Console.WriteLine($"DeleteProject: url = {url}");
            ResponseDto response = await this.SendAsync<ResponseDto>(new ApiRequest()
            {
                ApiType = SD.ApiType.DELETE,
                Url = url
            });
            if (!response.IsSuccess)
            {
                throw new Exception($"Error deleting index: {response.ErrorMessages}");
            }
        }
    }
}
