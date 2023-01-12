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
    public class IndexViewServerless : BaseService, IIndexView
    {
        private readonly IHttpClientFactory _clientFactory;

        public IndexViewServerless(IHttpClientFactory clientFactory) : base(clientFactory)
        {
        }

        public async Task<List<DmsIndex>> GetChildren(string source, int id)
        {
            List<DmsIndex> children = new List<DmsIndex>();
            string url = SD.IndexAPIBase.BuildFunctionUrl("/api/DmIndex", $"name={source}&id={id}", SD.IndexKey);
            Console.WriteLine($"GetDmIndexesAsync: url = {url}");
            ResponseDto response = await this.SendAsync<ResponseDto>(new ApiRequest()
            {
                ApiType = SD.ApiType.GET,
                Url = url
            });
            children = JsonConvert.DeserializeObject<List<DmsIndex>>(response.Result.ToString());
            return children;
        }

        public Task<List<DmsIndex>> GetIndex(string source)
        {
            throw new NotImplementedException();
        }

        public Task<List<IndexFileDefinition>> GetIndexFileDefs(string fileName)
        {
            throw new NotImplementedException();
        }

        public async Task<List<IndexFileData>> GetIndexTaxonomy(string source)
        {
            //List<IndexFileData> data = new List<IndexFileData>();
            string url = SD.IndexAPIBase.BuildFunctionUrl("/api/DmIndexes", $"Name={source}&Node=/&Level=0", SD.IndexKey);
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
    }
}
