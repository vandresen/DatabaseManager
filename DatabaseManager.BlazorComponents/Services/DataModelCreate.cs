using DatabaseManager.BlazorComponents.Extensions;
using DatabaseManager.Shared;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DatabaseManager.BlazorComponents.Services
{
    public class DataModelCreate : IDataModelCreate
    {
        private readonly IHttpService httpService;
        private string url;
        private string baseUrl;
        private readonly string apiKey;

        public DataModelCreate(IHttpService httpService, SingletonServices settings)
        {
            this.httpService = httpService;
            baseUrl = settings.BaseUrl;
            apiKey = settings.ApiKey;
        }

        public async Task Create(DataModelParameters modelParameters)
        {
            if (string.IsNullOrEmpty(baseUrl)) url = "api/datamodel";
            else url = baseUrl.BuildFunctionUrl("DataModelCreate", $"", apiKey);
            Console.WriteLine($"Create model: {url}");
            var response = await httpService.Post(url, modelParameters);
            if (!response.Success)
            {
                throw new ApplicationException(await response.GetBody());
            }
        }
    }
}
