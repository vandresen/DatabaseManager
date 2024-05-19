using Castle.Components.DictionaryAdapter;
using DatabaseManager.Services.DataOps.Extensions;
using DatabaseManager.Services.DataOps.Models;
using Google.Protobuf.WellKnownTypes;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;

namespace DatabaseManager.Services.DataOps.Services
{
    public class DataQc : BaseService, IDataQc
    {
        private readonly IConfiguration _configuration;
        private readonly IIndexAccess _indexAccess;
        private readonly IRuleAccess _ruleAccess;

        public DataQc(IConfiguration configuration,
            IIndexAccess indexAccess, IRuleAccess ruleAccess,
            IHttpClientFactory clientFactory, ILoggerFactory loggerFactory) : base(clientFactory)
        {
            _configuration = configuration;
            _indexAccess = indexAccess;
            _ruleAccess = ruleAccess;
        }

        public async Task<T> CloseDataQc<T>(string source, List<RuleFailures> ruleFailures)
        {
            string project = "";
            ResponseDto response = new ResponseDto();
            try
            {
                ResponseDto idxResponse = await _indexAccess.GetIndexes<ResponseDto>(source, project, "");
                ResponseDto ruleResponse = await _ruleAccess.GetRules<ResponseDto>(source);
                if (idxResponse.IsSuccess && ruleResponse.IsSuccess)
                {
                    var indexes = JsonConvert.DeserializeObject<List<IndexDto>>(Convert.ToString(idxResponse.Result));
                    foreach (var index in indexes)
                    {
                        index.QC_String = "";
                    }
                    var rules = JsonConvert.DeserializeObject<List<RuleModelDto>>(Convert.ToString(ruleResponse.Result));
                    foreach (var ruleFailure in ruleFailures)
                    {
                        RuleModelDto rule = rules.FirstOrDefault(o => o.Id == ruleFailure.RuleId);
                        foreach (var failure in ruleFailure.Failures)
                        {
                            IndexDto index = indexes.FirstOrDefault(o => o.IndexId == failure);
                            if (index == null)
                            {
                                Exception error = new Exception($"CloseDataQc: Cannot get the index items");
                                throw error;
                            }
                            string qcString = index.QC_String;
                            qcString = qcString + rule.RuleKey + ";";
                            index.QC_String = qcString;
                        }
                    }
                    ResponseDto updateRespones = await _indexAccess.UpdateIndexes<ResponseDto>(indexes, source, project);
                    if (updateRespones.IsSuccess) 
                    { 
                        response.IsSuccess = true; 
                    }
                    else
                    {
                        response.IsSuccess = false;
                        string error = $"CloseDataQc: Could not update indexes";
                        response.ErrorMessages = new List<string>() { error };
                    }
                }
                else
                {
                    response.IsSuccess = false;
                    string error = $"CloseDataQc: Could not close indexes";
                    response.ErrorMessages = new List<string>() { error };
                }
                return await Task.FromResult((T)(object)response);
            }
            catch (Exception ex)
            {
                Exception error = new Exception($"CloseDataQc: Could not close and save qc flags, {ex}");
                throw error;
            }
            
        }

        public async Task<T> ExecuteDataQc<T>(DataQCParameters qcParms)
        {
            var dataQCAPIBase = _configuration.GetValue<string>("DataQCAPI");
            var dataQCKey = _configuration.GetValue<string>("DataQCKey");
            string url = dataQCAPIBase.BuildFunctionUrl($"/DataQC", $"", dataQCKey);
            return await this.SendAsync<T>(new ApiRequest()
            {
                ApiType = SD.ApiType.POST,
                Data = qcParms,
                Url = url
            });
        }
    }
}
