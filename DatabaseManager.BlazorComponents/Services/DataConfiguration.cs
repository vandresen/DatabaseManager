using DatabaseManager.BlazorComponents.Extensions;
using DatabaseManager.BlazorComponents.Models;
using DatabaseManager.Shared;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace DatabaseManager.BlazorComponents.Services
{
    public class DataConfiguration : IDataConfiguration
    {
        private readonly IHttpService httpService;
        private readonly string folder = "connectdefinition";
        private string url;

        public DataConfiguration(IHttpService httpService)
        {
            this.httpService = httpService;
        }

        public async Task<ResponseDto> GetRecord(string name)
        {
            ResponseDto responseDto = new ResponseDto();
            if (string.IsNullOrEmpty(SD.DataConfigurationAPIBase)) url = $"api/DataConfiguration?folder={folder}&name={name}";
            else url = SD.DataConfigurationAPIBase.BuildFunctionUrl("/api/GetDataConfiguration", $"folder={folder}&name={name}", SD.DataConfigurationKey);
            Console.WriteLine(url);
            var response = await httpService.Get<ResponseDto>(url);
            if (!response.Success)
            {
                throw new ApplicationException(await response.GetBody());
            }
            return response.Response;
        }

        public async Task<ResponseDto> GetRecords()
        {
            ResponseDto responseDto = new ResponseDto();
            if (string.IsNullOrEmpty(SD.DataConfigurationAPIBase)) url = $"api/DataConfiguration?folder={folder}";
            else url = SD.DataConfigurationAPIBase.BuildFunctionUrl("/api/GetDataConfiguration", $"folder={folder}", SD.DataConfigurationKey);
            Console.WriteLine(url);
            var response = await httpService.Get<ResponseDto>(url);
            if (!response.Success)
            {
                throw new ApplicationException(await response.GetBody());
            }
            return response.Response;
        }
    }
}
