using DatabaseManager.Services.Reports.Extensions;
using DatabaseManager.Services.Reports.Models;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Runtime.Intrinsics.Arm;

namespace DatabaseManager.Services.Reports.Services
{
    public class IndexAccess : BaseService, IIndexAccess
    {
        private readonly IHttpClientFactory _clientFactory;
        private readonly ILogger<IndexAccess> _logger;
        private readonly IRuleAccess _ra;
        private readonly IConfiguration _configuration;
        private readonly IDatabaseAccess _db;

        public IndexAccess(IHttpClientFactory clientFactory, ILogger<IndexAccess> logger, IRuleAccess ra,
            IConfiguration configuration, IDatabaseAccess db) : base(clientFactory)
        {
            _clientFactory = clientFactory;
            _logger = logger;
            _ra = ra;
            _configuration = configuration;
            _db = db;
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

        public async Task InsertChildEdits(ReportData reportData, string dataSource, string project)
        {
            ResponseDto ruleResponse = await _ra.GetRules<ResponseDto>(dataSource);
            if (ruleResponse == null || !ruleResponse.IsSuccess)
            {
                Exception error = new Exception($"InsertChildEdits: Failed getting rules");
                throw error;
            }
            List<RuleModel> rules = JsonConvert.DeserializeObject<List<RuleModel>>(ruleResponse.Result.ToString());
            RuleModel rule = rules.FirstOrDefault(x => x.RuleKey == reportData.RuleKey);
            EntiretyParms parms = JsonConvert.DeserializeObject<EntiretyParms>(rule.RuleParameters);

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
            ResponseDto indexResponse = await this.SendAsync<ResponseDto>(new ApiRequest()
            {
                ApiType = SD.ApiType.GET,
                Url = url
            });
            if (indexResponse == null || !indexResponse.IsSuccess)
            {
                Exception error = new Exception($"InsertChildEdits: Failed getting the index");
                throw error;
            }
            IndexDto parentObject = JsonConvert.DeserializeObject<IndexDto>(indexResponse.Result.ToString());
            ResponseDto rootResponse = await GetRootIndex<ResponseDto>(dataSource, project);
            if (rootResponse == null || !rootResponse.IsSuccess)
            {
                Exception error = new Exception($"InsertChildEdits: Failed getting the root index");
                throw error;
            }
            _logger.LogInformation($"Root json = {rootResponse.Result}");
            List<DmsIndex> rootIndexes = JsonConvert.DeserializeObject<List<DmsIndex>>(rootResponse.Result.ToString());
            DmsIndex rootIndex = rootIndexes.FirstOrDefault();
            if (rootIndex == null)
            {
                Exception error = new Exception($"InsertChildEdits: Failed getting the root index");
                throw error;
            }
            _logger.LogInformation($"Root json {rootIndex.JsonData}");
            IndexRootJson rootJson = JsonConvert.DeserializeObject<IndexRootJson>(rootIndex.JsonData);
            DataAccessDef accessDef = rootJson.Source.GetDataAccessDefintionFromSourceJson(parms.DataType);
            string sourceConectionString = rootJson.Source.GetConnectionStringFromSourceJson();

            string taxonomy = rootJson.Taxonomy;
            JArray JsonIndexArray = JArray.Parse(taxonomy);
            List<IndexFileData> idxData = new List<IndexFileData>();
            foreach (JToken level in JsonIndexArray)
            {
                idxData.Add(ProcessJTokens(level));
                idxData = ProcessIndexArray(level, idxData);
            }
            IndexFileData taxonomyInfoForMissingObject = idxData.FirstOrDefault(x => x.DataName == parms.DataType);

            string dataTypeSql = accessDef.Select;
            string table = dataTypeSql.GetTable();


            IEnumerable<TableSchema> attributeProperties = await GetColumnInfo(sourceConectionString, table);

            string emptyJson = GetJsonForMissingDataObject(rule.RuleParameters, accessDef, attributeProperties);
            if (emptyJson == "Error")
            {
                throw new NullReferenceException("Could not create an empty json data object, maybe you are missing Datatype in parameters");
            }
            string json = UpdateJsonAttribute(emptyJson, taxonomyInfoForMissingObject.NameAttribute, parms.Name);
            json = PopulateJsonWithKeyValues(taxonomyInfoForMissingObject, json, parentObject, accessDef);
            await InsertNewObjectToIndex(json, parms.DataType, taxonomyInfoForMissingObject, parentObject, accessDef, dataSource, project);
            //if (connector.SourceType == "DataBase")
            //{
            //    await InsertNewObjectToDatabase(json, parms.DataType);
            //}
            string failRule = reportData.RuleKey + ";";
            parentObject.QC_String = parentObject.QC_String.Replace(failRule, "");
            List<IndexDto> updateIndexes = [parentObject];
            await UpdateIndex(updateIndexes, dataSource, "");
        }

        public async Task MergeIndexes(ReportData reportData, string dataSource, string project)
        {
            ResponseDto ruleResponse = await _ra.GetRules<ResponseDto>(dataSource);
            if (ruleResponse == null || !ruleResponse.IsSuccess)
            {
                Exception error = new Exception($"MergeIndexes: Failed getting rules");
                throw error;
            }
            List<RuleModel> rules = JsonConvert.DeserializeObject<List<RuleModel>>(ruleResponse.Result.ToString());
            RuleModel rule = rules.FirstOrDefault(x => x.RuleKey == reportData.RuleKey);

            ResponseDto duplicateResponse = await GetIndexFailures<ResponseDto>(dataSource, "", rule.DataType, reportData.RuleKey);
            if (duplicateResponse == null || !duplicateResponse.IsSuccess)
            {
                Exception error = new Exception($"MergeIndexes: Failed getting duplicates");
                throw error;
            }
            IEnumerable<IndexDto> duplicates = JsonConvert.DeserializeObject<IEnumerable<IndexDto>>(duplicateResponse.Result.ToString());
            reportData.TextValue = reportData.JsonData.GetUniqKey(rule);
            foreach (var duplicate in duplicates)
            {
                if (duplicate.IndexId != reportData.Id)
                {
                    string key = duplicate.JsonDataObject.GetUniqKey(rule);
                    if (key == reportData.TextValue)
                    {
                        _logger.LogInformation($"Found duplicate");
                        await MergeIndexChildren(dataSource, duplicate.IndexId,
                            reportData.Id, reportData.RuleKey);
                    }
                }
            }
        }

        private async Task MergeIndexChildren(string dataSource, int oldId, int newId, string ruleKey)
        {
            Dictionary<string, int> nodes = new Dictionary<string, int>();

            IEnumerable<IndexDto> newIndex = await GetDescendants(newId, dataSource, "");
            foreach (var index in newIndex)
            {
                if (string.IsNullOrEmpty(index.DataKey))
                {
                    nodes.Add(index.DataType, index.IndexId);
                }
            }

            IEnumerable<IndexDto> oldIndex = await GetDescendants(oldId, dataSource, "");
            IndexDto newParentIndex = newIndex.FirstOrDefault(x => x.IndexId == newId);
            IndexDto oldParentIndex = oldIndex.FirstOrDefault(x => x.IndexId == oldId);
            foreach (var index in oldIndex)
            {
                if (index.IndexId != oldId)
                {
                    string name = index.DataName;
                    IndexDto idx = newIndex.FirstOrDefault(x => x.DataName == name);
                    if (idx == null)
                    {
                        if (index.DataType != index.DataName)
                        {
                            string newJson = GetMergeJson(newParentIndex.DataKey, index.JsonDataObject);
                            int result = await InsertSingleIndex(newJson, newParentIndex.IndexId, "", dataSource, index.DataType);
                            index.JsonDataObject = "";
                            index.QC_String = "";
                            List<IndexDto> deleteIndexes = [index];
                            await UpdateIndex(deleteIndexes, dataSource, "");
                        }
                    }
                }
            }

            oldParentIndex.JsonDataObject = "";
            oldParentIndex.QC_String = "";
            List<IndexDto> oldParentIndexes = [oldParentIndex];
            await UpdateIndex(oldParentIndexes, dataSource, "");
            newParentIndex.QC_String = newParentIndex.QC_String.Replace((ruleKey + ";"), "");
            List<IndexDto> newParentIndexes = [newParentIndex];
            await UpdateIndex(newParentIndexes, dataSource, "");
        }

        private string GetMergeJson(string dataKey, string json)
        {
            string newJson = json;
            string[] keyAttributes = dataKey.Split(new string[] { "AND" }, StringSplitOptions.None);
            foreach (var keyAttribute in keyAttributes)
            {
                string key = keyAttribute.Trim();
                string[] keyPair = dataKey.Split('=');
                newJson = newJson.ModifyJson(keyPair[0].Trim(), keyPair[1].Trim());
            }
            return newJson;
        }

        private string GetMergeKey(string newParentDataKey, string oldParentDataKey, string oldIndexDataKey)
        {
            string key = oldIndexDataKey.Replace(oldParentDataKey, newParentDataKey);
            return key;
        }

        private async Task<IEnumerable<IndexDto>> GetDescendants(int indexId, string dataSource, string project)
        {
            string url = "";
            if (SD.Sqlite)
            {
                url = SD.IndexAPIBase.BuildFunctionUrl($"/GetDescendants/{indexId}", $"project={project}", SD.IndexKey);
            }
            else
            {
                url = SD.IndexAPIBase.BuildFunctionUrl($"/GetDescendants/{indexId}", $"Name={dataSource}", SD.IndexKey);
            }
            _logger.LogInformation($"Url = {url}");
            ResponseDto indexResponse = await this.SendAsync<ResponseDto>(new ApiRequest()
            {
                ApiType = SD.ApiType.GET,
                Url = url
            });
            if (indexResponse == null || !indexResponse.IsSuccess)
            {
                Exception error = new Exception($"GetDescendants: Failed saving index, {indexResponse.ErrorMessages}");
                throw error;
            }
            else
            {
                IEnumerable<IndexDto> indexes = JsonConvert.DeserializeObject<IEnumerable<IndexDto>>(indexResponse.Result.ToString());
                return indexes;
            }
        }

        private async Task InsertNewObjectToIndex(string json, string dataType, IndexFileData taxonomyInfoForMissingObject,
            IndexDto parentObject, DataAccessDef accessDef, string dataSource, string project)
        {
            JObject dataObject = JObject.Parse(json);
            string dataName = dataObject[taxonomyInfoForMissingObject.NameAttribute].ToString();
            string dataKey = GetDataKey(dataObject, accessDef.Keys);
            int parentId = parentObject.IndexId;
            int result = await InsertSingleIndex(json, parentId, "", dataSource, dataType);
        }

        private async Task<int> InsertSingleIndex(string json, int parentId, string project, string dataSource, string dataType)
        {
            dynamic dataObject = JsonConvert.DeserializeObject<dynamic>(json);
            string url = "";
            if (SD.Sqlite)
            {
                url = SD.IndexAPIBase.BuildFunctionUrl($"/Indexes", $"project={project}&Parentid={parentId}&Datatype={dataType}", SD.IndexKey);
            }
            else
            {
                url = SD.IndexAPIBase.BuildFunctionUrl($"/Indexes", $"Name={dataSource}&Parentid={parentId}&Datatype={dataType}", SD.IndexKey);
            }
            _logger.LogInformation($"Url = {url}");
            ResponseDto indexResponse = await this.SendAsync<ResponseDto>(new ApiRequest()
            {
                ApiType = SD.ApiType.POST,
                Data = dataObject,
                Url = url
            });
            if (indexResponse == null || !indexResponse.IsSuccess)
            {
                Exception error = new Exception($"GetDescendants: Failed saving index, {indexResponse.ErrorMessages}");
                throw error;
            }
            else
            {
                int id = Convert.ToInt32(indexResponse.Result);
                return id;
            }
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

        private static IndexFileData ProcessJTokens(JToken token)
        {
            IndexFileData idxDataObject = new IndexFileData();
            idxDataObject.DataName = (string)token["DataName"];
            idxDataObject.NameAttribute = token["NameAttribute"]?.ToString();
            idxDataObject.LatitudeAttribute = token["LatitudeAttribute"]?.ToString();
            idxDataObject.LongitudeAttribute = token["LongitudeAttribute"]?.ToString();
            idxDataObject.ParentKey = token["ParentKey"]?.ToString();
            if (token["UseParentLocation"] != null) idxDataObject.UseParentLocation = (Boolean)token["UseParentLocation"];
            if (token["Arrays"] != null)
            {
                idxDataObject.Arrays = token["Arrays"];
            }
            return idxDataObject;
        }

        private List<IndexFileData> ProcessIndexArray(JToken parent, List<IndexFileData> idxData)
        {
            List<IndexFileData> result = idxData;
            if (parent["DataObjects"] != null)
            {
                foreach (JToken level in parent["DataObjects"])
                {
                    result.Add(ProcessJTokens(level));
                    result = ProcessIndexArray(level, result);
                }
            }
            return result;
        }

        private static string GetJsonForMissingDataObject(string parameters, DataAccessDef accessDef,
            IEnumerable<TableSchema> attributeProperties)
        {
            string json = "{}";
            MissingObjectsParameters missingObjectParms = new MissingObjectsParameters();
            missingObjectParms = JsonConvert.DeserializeObject<MissingObjectsParameters>(parameters);

            string[] columns = accessDef.Select.GetAttributes();
            JObject dataObject = JObject.Parse(json);
            foreach (string column in columns)
            {
                TableSchema tableSchema = attributeProperties.FirstOrDefault(x => x.COLUMN_NAME == column.Trim());
                if (tableSchema == null)
                {
                    break;
                }
                else
                {
                    string type = tableSchema.TYPE_NAME.ToLower();
                    if (type == "numeric")
                    {
                        dataObject[column.Trim()] = -99999.0;
                    }
                    else if (type == "datetime")
                    {
                        dataObject[column.Trim()] = DateTime.Now.ToString("yyyy-MM-dd");
                    }
                    else
                    {
                        dataObject[column.Trim()] = "";
                    }
                }
            }
            json = dataObject.ToString();

            return json;
        }

        private string PopulateJsonWithKeyValues(IndexFileData taxonomyInfo, string emptyJson, IndexDto parentObject, DataAccessDef accessDef)
        {
            string json = emptyJson;
            string[] parentKeys = taxonomyInfo.ParentKey.Split(',').Select(key => key.Trim()).ToArray();
            foreach (var parentKey in parentKeys)
            {
                int start = parentKey.IndexOf("[") + 1;
                int to = parentKey.IndexOf("]");
                int end = parentKey.IndexOf("=");
                if (start < 0 || to < 0)
                {
                    throw new Exception($"ReportEditManagement: Parent key definition in Taxonomy is not valid");
                }
                string attribute = parentKey.Substring(start, (to - start));
                attribute = attribute.Trim();
                string key = parentKey.Substring(0, end).Trim();
                JObject jsonObject = JObject.Parse(parentObject.JsonDataObject);
                string name = (string)jsonObject[attribute];
                json = UpdateJsonAttribute(json, key, name);
            }

            if (string.IsNullOrEmpty(accessDef.Constants) == false)
            {
                Dictionary<string, string> constants = accessDef.Constants.ToStringDictionary();
                foreach (var kvp in constants)
                {
                    json = UpdateJsonAttribute(json, kvp.Key, kvp.Value);
                }
            }

            JObject obj = JObject.Parse(json);
            string[] keys = accessDef.Keys.Split(',').Select(key => key.Trim()).ToArray();
            foreach (string key in keys)
            {
                if (string.IsNullOrEmpty(obj[key].ToString()) == true)
                {
                    throw new Exception($"ReportEditManagement: Not all keys have a value");
                }
            }

            return json;
        }

        private string UpdateJsonAttribute<T>(string json, string attributeName, T newValue)
        {
            JObject jsonObject = JObject.Parse(json);
            jsonObject[attributeName] = JToken.FromObject(newValue);
            return jsonObject.ToString(Formatting.None);
        }

        private string GetDataKey(JObject dataObject, string dbKeys)
        {
            string dataKey = "";
            string and = "";
            string[] keys = dbKeys.Split(',');
            foreach (string key in keys)
            {
                string attribute = key.Trim();
                string attributeValue = "'" + dataObject[attribute].ToString() + "'";
                dataKey = dataKey + and + key.Trim() + " = " + attributeValue;
                and = " AND ";
            }
            return dataKey;
        }

        private async Task UpdateIndex(List<IndexDto> updateIndexes, string dataSource, string project)
        {
            string url = "";
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
        }

        private Task<IEnumerable<TableSchema>> GetColumnInfo(string connectionString, string table) =>
            _db.LoadData<TableSchema, dynamic>("dbo.sp_columns", new { TABLE_NAME = table }, connectionString);

        private class EntiretyParms
        {
            public string DataType { get; set; }
            public string Name { get; set; }
        }

        public class MissingObjectsParameters
        {
            public string DataType { get; set; }
            public List<MissingObjectKey> Keys { get; set; }
            public List<MissingObjectDefault> Defaults { get; set; }
        }

        public class MissingObjectDefault
        {
            public string Default { get; set; }
            public string Value { get; set; }
        }

        public class MissingObjectKey
        {
            public string Key { get; set; }
            public string Value { get; set; }
        }
    }
}
