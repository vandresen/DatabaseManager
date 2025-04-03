using DatabaseManager.ServerLessClient.Models;
using DatabaseManager.ServerLessClient.Helpers;
using Newtonsoft.Json;

namespace DatabaseManager.ServerLessClient.Services
{
    public class ReportService : BaseService, IReport
    {
        string baseUrl;
        string apiKey;
        private readonly IHttpClientFactory _clientFactory;

        public ReportService(IHttpClientFactory clientFactory, IConfiguration configuration) : base(clientFactory)
        {
            baseUrl = configuration["ServiceUrls:ReportApiBase"];
            apiKey = configuration["ServiceUrls: ReportKey"];
        }

        public async Task<List<QcResult>> GetResults(string source)
        {
            List<QcResult> result = new List<QcResult>();
            string url = baseUrl.BuildFunctionUrl("GetResults", $"name={source}", apiKey);
            Console.WriteLine(url);
            ResponseDto response = await this.SendAsync<ResponseDto>(new ApiRequest()
            {
                ApiType = SD.ApiType.GET,
                Url = url
            });
            result = JsonConvert.DeserializeObject<List<QcResult>>(response.Result.ToString());
            return result;
        }

        public async Task<List<TableSchema>> GetAttributeInfo(string source, string dataType)
        {
            List<TableSchema> result = new List<TableSchema>();
            string url = baseUrl.BuildFunctionUrl("ReportAttributeInfo", $"Name={source}&Datatype={dataType}", apiKey);
            Console.WriteLine(url);
            ResponseDto response = await this.SendAsync<ResponseDto>(new ApiRequest()
            {
                ApiType = SD.ApiType.GET,
                Url = url
            });
            result = JsonConvert.DeserializeObject<List<TableSchema>>(response.Result.ToString());
            return result;
        }

        public async Task<List<DmsIndex>> GetResult(string source, int id)
        {
            List<DmsIndex> result = new List<DmsIndex>();
            string url = baseUrl.BuildFunctionUrl("GetResult", $"name={source}&Id={id}", apiKey);
            Console.WriteLine(url);
            ResponseDto response = await this.SendAsync<ResponseDto>(new ApiRequest()
            {
                ApiType = SD.ApiType.GET,
                Url = url
            });
            result = JsonConvert.DeserializeObject<List<DmsIndex>>(response.Result.ToString());
            return result;
        }

        public async Task Update(string source, ReportData reportData)
        {
            string url = baseUrl.BuildFunctionUrl("UpdateReportData", $"name={source}", apiKey);
            Console.WriteLine(url);
            ResponseDto response = await this.SendAsync<ResponseDto>(new ApiRequest()
            {
                ApiType = SD.ApiType.PUT,
                Url = url,
                Data = reportData
            });
            if (!response.IsSuccess)
            {
                throw new ApplicationException(String.Join("Update data objects:", response.ErrorMessages));
            }
        }

        public async Task Delete(int id, string source)
        {
            string url = baseUrl.BuildFunctionUrl("DeleteReportData", $"name={source}&Id={id}", apiKey);
            Console.WriteLine(url);
            ResponseDto response = await this.SendAsync<ResponseDto>(new ApiRequest()
            {
                ApiType = SD.ApiType.DELETE,
                Url = url
            });
            if (!response.IsSuccess)
            {
                throw new ApplicationException(String.Join("Delete data objects:", response.ErrorMessages));
            }
        }

        public async Task InsertChild(string source, ReportData reportData)
        {
            string url = baseUrl.BuildFunctionUrl("InsertChildReportData", $"name={source}", apiKey);
            Console.WriteLine(url);
            ResponseDto response = await this.SendAsync<ResponseDto>(new ApiRequest()
            {
                ApiType = SD.ApiType.POST,
                Url = url,
                Data = reportData
            });
            if (!response.IsSuccess)
            {
                throw new ApplicationException(String.Join("Update data objects:", response.ErrorMessages));
            }
        }
    }
}
