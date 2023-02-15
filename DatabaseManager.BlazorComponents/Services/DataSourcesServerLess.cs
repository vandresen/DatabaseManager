using DatabaseManager.BlazorComponents.Extensions;
using DatabaseManager.BlazorComponents.Models;
using DatabaseManager.Shared;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DatabaseManager.BlazorComponents.Services
{
    public class DataSourcesServerLess : IDataSources
    {
        private readonly IHttpService httpService;
        private readonly IDataSourceService _ds;
        private readonly string baseUrl;
        private readonly string apiKey;

        public DataSourcesServerLess(IHttpService httpService, SingletonServices settings, IDataSourceService ds)
        {
            this.httpService = httpService;
            _ds = ds;
            baseUrl = settings.BaseUrl;
            apiKey = settings.ApiKey;
        }
        public async Task CreateSource(ConnectParameters connectParameters)
        {
            ResponseDto response = await _ds.CreateDataSourceAsync<ResponseDto>(connectParameters);
            if (!response.IsSuccess)
            {
                throw new ApplicationException(String.Join("; ", response.ErrorMessages));
            }
        }

        public async Task DeleteSource(string Name)
        {
            ResponseDto response = await _ds.DeleteDataSourceAsync<ResponseDto>(Name);
            if (!response.IsSuccess)
            {
                throw new ApplicationException(String.Join("; ", response.ErrorMessages));
            }
        }

        public async Task<ConnectParameters> GetSource(string Name)
        {
            ResponseDto response = await _ds.GetDataSourceByNameAsync<ResponseDto>(Name);
            if (response.IsSuccess)
            {
                return JsonConvert.DeserializeObject<ConnectParameters>(Convert.ToString(response.Result));
            }
            else
            {
                throw new ApplicationException(String.Join("; ", response.ErrorMessages));
            }
        }

        public async Task<List<ConnectParameters>> GetSources()
        {
            ResponseDto response = await _ds.GetAllDataSourcesAsync<ResponseDto>();
            if(response.IsSuccess) 
            { 
                return JsonConvert.DeserializeObject<List<ConnectParameters>>(Convert.ToString(response.Result));
            }
            else
            {
                throw new ApplicationException(String.Join("; ", response.ErrorMessages));
            }
        }

        public async Task UpdateSource(ConnectParameters connectParameters)
        {
            ResponseDto response = await _ds.CreateDataSourceAsync<ResponseDto>(connectParameters);
            if (!response.IsSuccess)
            {
                throw new ApplicationException(String.Join("; ", response.ErrorMessages));
            }
        }
    }
}
