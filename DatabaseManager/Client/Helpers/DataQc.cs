﻿using DatabaseManager.Common.Services;
using DatabaseManager.Shared;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace DatabaseManager.Client.Helpers
{
    public class DataQc: IDataQc
    {
        private readonly IHttpService httpService;
        private string url = "api/DataQC";

        public DataQc(IHttpService httpService)
        {
            this.httpService = httpService;
        }

        public async Task<List<QcResult>> GetQcResult(string source)
        {
            var response = await httpService.Get<List<QcResult>>($"{url}/{source}");
            if (!response.Success)
            {
                throw new ApplicationException(await response.GetBody());
            }
            return response.Response;
        }

        public async Task<List<DmsIndex>> GetQcFailures(string source, int id)
        {
            var response = await httpService.Get<List<DmsIndex>>($"{url}/{source}/{id}");
            if (!response.Success)
            {
                throw new ApplicationException(await response.GetBody());
            }
            return response.Response;
        }

        public async Task ClearQCFlags(string source)
        {
            var response = await httpService.Post($"{url}/ClearQCFlags/{source}");
            if (!response.Success)
            {
                throw new ApplicationException(await response.GetBody());
            }
        }

        public async Task ProcessQCRule(DataQCParameters qcParams)
        {
            var response = await httpService.Post(url, qcParams);
            if (!response.Success)
            {
                throw new ApplicationException(await response.GetBody());
            }
        }
    }
}
