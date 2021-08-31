using DatabaseManager.Common.Services;
using DatabaseManager.Shared;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace DatabaseManager.Client.Helpers
{
    public class DataModelCreate : IDataModelCreate
    {
        private readonly IHttpService httpService;
        private string modelUrl = "api/datamodel";

        public DataModelCreate(IHttpService httpService)
        {
            this.httpService = httpService;
        }

        public async Task Create(DataModelParameters modelParameters)
        {
            var response = await httpService.Post(modelUrl, modelParameters);
            if (!response.Success)
            {
                throw new ApplicationException(await response.GetBody());
            }
        }
    }
}
