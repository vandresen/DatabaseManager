using DatabaseManager.BlazorComponents.Extensions;
using DatabaseManager.Shared;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace DatabaseManager.BlazorComponents.Services
{
    public class IndexView : IIndexView
    {
        private readonly IHttpService httpService;
        private string url;
        private string baseUrl;
        private string apiKey;

        public IndexView(IHttpService httpService, SingletonServices settings)
        {
            this.httpService = httpService;
            baseUrl = settings.BaseUrl;
            apiKey = settings.ApiKey;
            Console.WriteLine($"Init index view: {baseUrl}");
        }

        public async Task<List<DmsIndex>> GetChildren(string source, int id)
        {
            url = $"api/index/{source}/{id}";
            var response = await httpService.Get<List<DmsIndex>>(url);
            if (!response.Success)
            {
                throw new ApplicationException(await response.GetBody());
            }
            return response.Response;
        }

        public async Task<List<DmsIndex>> GetIndex(string source)
        {
            Console.WriteLine($"Get index file defs base url: {baseUrl}");
            if (string.IsNullOrEmpty(baseUrl)) url = $"api/index/{source}";
            else url = baseUrl.BuildFunctionUrl("GetIndexData", $"name={source}", apiKey);
            var response = await httpService.Get<List<DmsIndex>>(url);
            if (!response.Success)
            {
                throw new ApplicationException(await response.GetBody());
            }
            return response.Response;
        }

        public async Task<List<IndexFileDefinition>> GetIndexFileDefs(string fileName)
        {
            url = $"api/index/GetTaxonomyFile/{fileName}";
            Console.WriteLine($"Save index url: {url}");
            var response = await httpService.Get<List<IndexFileDefinition>>(url);
            if (!response.Success)
            {
                throw new ApplicationException(await response.GetBody());
            }
            return response.Response;
        }

        public async Task SaveIndexFileDefs(List<IndexFileDefinition> indexDef, string fileName)
        {
            url = $"api/index/{fileName}";
            Console.WriteLine($"Save index url: {url}");
            var response = await httpService.Post(url, indexDef);
            if (!response.Success)
            {
                throw new ApplicationException(await response.GetBody());
            }
        }

        public async Task<List<IndexFileData>> GetIndexTaxonomy(string source)
        {
            url = $"api/index/GetIndexTaxonomy/{source}";
            var response = await httpService.Get<List<IndexFileData>>(url);
            if (!response.Success)
            {
                throw new ApplicationException(await response.GetBody());
            }
            return response.Response;
        }

        public async Task<IndexModel> GetSingleIndexItem(string source, int id)
        {
            url = $"api/index/GetSingleIndexItem/{source}/{id}";
            var response = await httpService.Get<IndexModel>(url);
            if (!response.Success)
            {
                throw new ApplicationException(await response.GetBody());
            }
            return response.Response;
        }

        public void InitSettings(SingletonServices settings)
        {
            baseUrl = settings.BaseUrl;
            apiKey = settings.ApiKey;
        }

        public async Task<IndexModel> GetIndexroot(string source)
        {
            if (string.IsNullOrEmpty(baseUrl)) url = $"api/index/GetIndexRoot/{source}";
            else url = baseUrl.BuildFunctionUrl("GetIndexRoot", $"name={source}", apiKey);
            var response = await httpService.Get<IndexModel>(url);
            if (!response.Success)
            {
                throw new ApplicationException(await response.GetBody());
            }
            return response.Response;
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
