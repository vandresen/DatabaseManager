using DatabaseManager.Shared;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace DatabaseManager.Common.Services
{
    public class DataOpsClientService : IDataOps
    {
        private readonly IHttpService httpService;
        private string url = "api/dataops";

        public DataOpsClientService(IHttpService httpService)
        {
            this.httpService = httpService;
        }

        public async Task CreatePipeline(DataOpsPipes pipe)
        {
            string name = pipe.Name;
            var response = await httpService.Post($"{url}/CreatePipeline/{name}");
            if (!response.Success) throw new ApplicationException(await response.GetBody());
        }

        public async Task SavePipeline(DataOpsPipes pipe, List<PipeLine> tubes)
        {
            string name = pipe.Name;
            var response = await httpService.Post($"{url}/SavePipeline/{name}", tubes);
            if (!response.Success) throw new ApplicationException(await response.GetBody());
        }
        public async Task DeletePipeline(string name)
        {
            var response = await httpService.Delete($"{url}/{name}");
            if (!response.Success)
            {
                throw new ApplicationException(await response.GetBody());
            }
        }

        public async Task<List<PipeLine>> GetPipeline(string name)
        {
            var response = await httpService.Get<List<PipeLine>>($"{url}/{name}");
            if (!response.Success)
            {
                throw new ApplicationException(await response.GetBody());
            }
            return response.Response;
        }

        public async Task<List<DataOpsPipes>> GetPipelines()
        {
            var response = await httpService.Get<List<DataOpsPipes>>($"{url}");
            if (!response.Success)
            {
                throw new ApplicationException(await response.GetBody());
            }
            return response.Response;
        }

        public async Task ProcessPipeline(List<DataOpParameters> parms)
        {
            var response = await httpService.Post(url, parms);
            if (!response.Success)
            {
                throw new ApplicationException(await response.GetBody());
            }
        }
    }
}
