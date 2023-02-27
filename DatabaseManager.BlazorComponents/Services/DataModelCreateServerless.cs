using DatabaseManager.BlazorComponents.Models;
using DatabaseManager.Shared;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DatabaseManager.BlazorComponents.Services
{
    public class DataModelCreateServerless : IDataModelCreate
    {
        private readonly IHttpService httpService;
        private readonly IDataModelService _dms;

        public DataModelCreateServerless(IHttpService httpService, IDataModelService dms)
        {
            this.httpService = httpService;
            _dms = dms;
        }

        public async Task Create(DataModelParameters modelParameters)
        {
            ResponseDto response = await _dms.Create<ResponseDto>(modelParameters);
            if (!response.IsSuccess)
            {
                throw new ApplicationException(String.Join("; ", response.ErrorMessages));
            }
        }
    }
}
