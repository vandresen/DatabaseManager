using DatabaseManager.Shared;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace DatabaseManager.Client.Helpers
{
    public class DatabaseTransfer: IDatabaseTransfer
    {
        private readonly IHttpService httpService;
        private string deleteUrl = "api/delete";
        private string copyUrl = "api/copy";

        public DatabaseTransfer(IHttpService httpService)
        {
            this.httpService = httpService;
        }

        public async Task Delete(TransferParameters transferParameters)
        {
            var response = await httpService.Post(deleteUrl, transferParameters);
            if (!response.Success)
            {
                throw new ApplicationException(await response.GetBody());
            }
        }

        public async Task Copy(TransferParameters transferParameters)
        {
            var response = await httpService.Post(copyUrl, transferParameters);
            if (!response.Success)
            {
                throw new ApplicationException(await response.GetBody());
            }
        }
    }
}
