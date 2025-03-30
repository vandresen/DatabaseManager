using DatabaseManager.Services.Reports.Extensions;
using DatabaseManager.Services.Reports.Models;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Runtime.Intrinsics.Arm;

namespace DatabaseManager.Services.Reports.Services
{
    public class IndexAccess : BaseService, IIndexAccess
    {
        private readonly IHttpClientFactory _clientFactory;
        private readonly ILogger<IndexAccess> _logger;
        private readonly IRuleAccess _ra;
        private readonly IConfiguration _configuration;
        private readonly IConfigFileService _cfs;
        private readonly IDatabaseAccess _db;

        public IndexAccess(IHttpClientFactory clientFactory, ILogger<IndexAccess> logger, IRuleAccess ra,
            IConfiguration configuration, IConfigFileService cfs, IDatabaseAccess db) : base(clientFactory)
        {
            _clientFactory = clientFactory;
            _logger = logger;
            _ra = ra;
            _configuration = configuration;
            _cfs = cfs;
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
            _logger.LogInformation($"Root source {rootJson.Source}");
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
            await InsertNewObjectToIndex(json, parms.DataType, taxonomyInfoForMissingObject, parentObject, accessDef);
            //if (connector.SourceType == "DataBase")
            //{
            //    await InsertNewObjectToDatabase(json, parms.DataType);
            //}
            //string failRule = reportData.RuleKey + ";";
            //parentObject.QC_String = parentObject.QC_String.Replace(failRule, "");
            //await _indexData.UpdateIndex(parentObject, connector.ConnectionString);

            throw new NotImplementedException();
        }

        private async Task InsertNewObjectToIndex(string json, string dataType, IndexFileData taxonomyInfoForMissingObject,
            IndexDto parentObject, DataAccessDef accessDef)
        {
            JObject dataObject = JObject.Parse(json);
            string dataName = dataObject[taxonomyInfoForMissingObject.NameAttribute].ToString();
            string dataKey = GetDataKey(dataObject, accessDef.Keys);
            int parentId = parentObject.IndexId;
            double latitude = -99999.0;
            double longitude = -99999.0;
            if (taxonomyInfoForMissingObject.UseParentLocation)
            {
                if (parentObject.Latitude != null) latitude = (double)parentObject.Latitude;
                if (parentObject.Longitude != null) longitude = (double)parentObject.Longitude;
            }
            int nodeId = await GetIndextNode(dataType, parentObject);
            //IndexModel indexModel = new IndexModel();
            //indexModel.Latitude = latitude;
            //indexModel.Longitude = longitude;
            //indexModel.DataType = dataType;
            //indexModel.DataName = dataName;
            //indexModel.DataKey = dataKey;
            //indexModel.JsonDataObject = json;
            //if (nodeId > 0)
            //{
            //    int result = await _indexData.InsertSingleIndex(indexModel, nodeId, _dbConnectionString);
            //}
        }

        private async Task<int> GetIndextNode(string dataType, IndexDto idxResult)
        {
            int nodeid = 0;
            string nodeName = dataType + "s";
            if (idxResult != null)
            {
                //IEnumerable<IndexDto> indexes = await _indexData.GetDescendantsFromSP(idxResult.IndexId, _dbConnectionString);
                //if (indexes.Count() > 1)
                //{
                //    var nodeIndex = indexes.FirstOrDefault(x => x.DataType == nodeName);
                //    if (nodeIndex != null)
                //    {
                //        nodeid = nodeIndex.IndexId;
                //    }
                //}
                if (nodeid == 0)
                {
                    //IndexModel nodeIndex = new IndexModel();
                    //nodeIndex.Latitude = 0.0;
                    //nodeIndex.Longitude = 0.0;
                    //nodeIndex.DataType = nodeName;
                    //nodeIndex.DataName = nodeName;
                    //nodeid = await _indexData.InsertSingleIndex(nodeIndex, idxResult.IndexId, _dbConnectionString);
                }
            }
            return nodeid;
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

        //private static string PopulateJsonForMissingDataObject(string parameters, string emptyJson, string parentData)
        //{
        //    string json = emptyJson;
        //    try
        //    {
        //        MissingObjectsParameters missingObjectParms = new MissingObjectsParameters();
        //        missingObjectParms = JsonConvert.DeserializeObject<MissingObjectsParameters>(parameters);
        //        JObject dataObject = JObject.Parse(emptyJson);
        //        JObject parentObject = JObject.Parse(parentData);
        //        foreach (var key in missingObjectParms.Keys)
        //        {
        //            var tagName = key.Key;
        //            var variable = key.Value;
        //            if (variable.Substring(0, 1) == "!")
        //            {
        //                string parentTag = key.Value.Substring(1);
        //                variable = parentObject[parentTag].ToString();
        //            }
        //            dataObject[tagName] = variable;
        //        }
        //        dataObject["REMARK"] = $"Has been predicted and created by QCEngine;";
        //        json = dataObject.ToString();
        //    }
        //    catch (Exception ex)
        //    {
        //        json = "Error";
        //    }

        //    return json;
        //}

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
