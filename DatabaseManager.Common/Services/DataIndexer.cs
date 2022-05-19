using DatabaseManager.Common.Extensions;
using DatabaseManager.Shared;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace DatabaseManager.Common.Services
{
    public class DataIndexer : IDataIndexer
    {
        private readonly IHttpService httpService;
        private string url;
        private string baseUrl;
        private readonly string apiKey;

        public DataIndexer(IHttpService httpService, SingletonServices settings)
        {
            this.httpService = httpService;
            baseUrl = settings.BaseUrl;
            apiKey = settings.ApiKey;
        }
        public async Task Create(CreateIndexParameters iParameters)
        {
            if (string.IsNullOrEmpty(baseUrl)) url = "api/createindex";
            else url = baseUrl.BuildFunctionUrl("CreateIndex", $"", apiKey);
            Console.WriteLine($"Create index: {url}");
            var response = await httpService.Post(url, iParameters);
            if (!response.Success)
            {
                throw new ApplicationException(await response.GetBody());
            }
        }

        public async Task<List<IndexFileList>> GetTaxonomies()
        {
            if (string.IsNullOrEmpty(baseUrl)) url = "api/createindex";
            else url = baseUrl.BuildFunctionUrl("GetTaxonomies", $"", apiKey);
            var response = await httpService.Get<List<IndexFileList>>(url);
            if (!response.Success)
            {
                throw new ApplicationException(await response.GetBody());
            }
            return response.Response;
        }

        public Task<CreateIndexParameters> GetTaxonomy(string Name)
        {
            throw new NotImplementedException();
        }
    }
}
