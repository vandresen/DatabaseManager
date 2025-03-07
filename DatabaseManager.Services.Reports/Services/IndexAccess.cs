using DatabaseManager.Services.Reports.Extensions;
using DatabaseManager.Services.Reports.Models;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.ComponentModel.DataAnnotations;

namespace DatabaseManager.Services.Reports.Services
{
    public class IndexAccess : BaseService, IIndexAccess
    {
        private readonly IHttpClientFactory _clientFactory;
        private readonly ILogger<IndexAccess> _logger;

        public IndexAccess(IHttpClientFactory clientFactory, ILogger<IndexAccess> logger) : base(clientFactory)
        {
            _clientFactory = clientFactory;
            _logger = logger;
        }

        public async Task<T> DeleteEdits<T>(int id, string dataSource, string project)
        {
            string url = SD.IndexAPIBase.BuildFunctionUrl($"/Indexes/{id}", $"Name={dataSource}&Project={project}", SD.IndexKey);
            return await this.SendAsync<T>(new ApiRequest()
            {
                ApiType = SD.ApiType.DELETE,
                Url = url
            });
        }

        public async Task<T> GetIndexFailures<T>(string dataSource, string project, string dataType, string qcString)
        {
            string url = SD.IndexAPIBase.BuildFunctionUrl($"/QueryIndex", 
                $"Name={dataSource}&DataType={dataType}&Project={project}&QcString={qcString}", SD.IndexKey);
            return await this.SendAsync<T>(new ApiRequest()
            {
                ApiType = SD.ApiType.GET,
                Url = url
            });
        }

        public async Task<T> GetRootIndex<T>(string dataSource, string project)
        {
            string url = "";
            if (SD.Sqlite)
            {
                url = SD.IndexAPIBase.BuildFunctionUrl($"/Index/1", $"project={project}", SD.IndexKey);
            }
            else
            {
                url = SD.IndexAPIBase.BuildFunctionUrl("/DmIndexes", $"Name={dataSource}&Node=/&Level=0", SD.IndexKey);
            }
            _logger.LogInformation($"Url = {url}");
            return await this.SendAsync<T>(new ApiRequest()
            {
                ApiType = SD.ApiType.GET,
                Url = url
            });
        }

        public async Task InsertEdits(ReportData reportData, string dataSource, string project)
        {
            string url = "";
            if (SD.Sqlite)
            {
                url = SD.IndexAPIBase.BuildFunctionUrl($"/Index/{reportData.Id}", $"project={project}", SD.IndexKey);
            }
            else
            {
                url = SD.IndexAPIBase.BuildFunctionUrl($"/Indexes/{reportData.Id}", $"Name={dataSource}", SD.IndexKey);
            }
            _logger.LogInformation($"Url = {url}");
            ResponseDto indexResponse =  await this.SendAsync<ResponseDto>(new ApiRequest()
            {
                ApiType = SD.ApiType.GET,
                Url = url
            });
            if (indexResponse == null || !indexResponse.IsSuccess)
            {
                Exception error = new Exception($"InsertEdits: Failed getting the index");
                throw error;
            }

            IndexDto index = JsonConvert.DeserializeObject<IndexDto>(indexResponse.Result.ToString());
            JObject dataObject = JObject.Parse(reportData.JsonData);

            if (reportData.ColumnType == "number")
            {
                dataObject[reportData.ColumnName] = reportData.NumberValue;
            }
            else if (reportData.ColumnType == "text")
            {
                dataObject[reportData.ColumnName] = reportData.TextValue;
            }
            else
            {
                Exception error = new Exception($"InsertEdits: Column type {reportData.ColumnType} not supported");
                throw error;
            }
            string remark = dataObject["REMARK"] + $";{reportData.ColumnName} has been manually edited;";
            dataObject["REMARK"] = remark;
            index.JsonDataObject = dataObject.ToString();
            string failRule = reportData.RuleKey + ";";
            index.QC_String = index.QC_String.Replace(failRule, "");
            List<IndexDto> updateIndexes = [index];

            if (SD.Sqlite)
            {
                url = SD.IndexAPIBase.BuildFunctionUrl($"/Indexes", $"project={project}", SD.IndexKey);
            }
            else
            {
                url = SD.IndexAPIBase.BuildFunctionUrl($"/Indexes", $"Name={dataSource}", SD.IndexKey);
            }
            _logger.LogInformation($"Url = {url}");
            ResponseDto updateResponse = await SendAsync<ResponseDto>(new ApiRequest()
            {
                ApiType = SD.ApiType.PUT,
                Url = url,
                Data = updateIndexes
            });
            if (updateResponse == null || !updateResponse.IsSuccess)
            {
                Exception error = new Exception($"InsertEdits: Failed to update index");
                throw error;
            }
            //if (connector.SourceType == "DataBase") UpdateDatabase(index.JsonDataObject, connector.ConnectionString, reportData.DataType);
        }
    }
}
