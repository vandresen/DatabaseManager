using System.Net;
using DatabaseManager.Services.Index.Extensions;
using DatabaseManager.Services.Index.Helpers;
using DatabaseManager.Services.Index.Models;
using DatabaseManager.Services.Index.Services;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace DatabaseManager.Services.Index
{
    public class Indexes
    {
        private readonly ILogger _logger;
        private readonly ILoggerFactory loggerFactory;
        private readonly IConfiguration _configuration;
        private readonly IDataSourceService _ds;
        private readonly IIndexDBAccess _indexDB;
        private readonly IFileStorageService _fs;
        protected ResponseDto _response;

        public Indexes(ILoggerFactory loggerFactory, IConfiguration configuration, 
            IDataSourceService ds, IIndexDBAccess indexDB, IFileStorageService fs)
        {
            _logger = loggerFactory.CreateLogger<Indexes>();
            this._response = new ResponseDto();
            this.loggerFactory = loggerFactory;
            _configuration = configuration;
            _ds = ds;
            _indexDB = indexDB;
            _fs = fs;
            SD.DataSourceAPIBase = _configuration.GetValue<string>("DataSourceAPI");
            SD.DataSourceKey = _configuration["DataSourceKey"];
        }

        [Function("CreateDatabase")]
        public async Task<ResponseDto> CreateDatabase(
            [HttpTrigger(AuthorizationLevel.Function, "post")] HttpRequestData req)
        {
            _logger.LogInformation("CreateDatabase: Starting.");

            try
            {
                var stringBody = await new StreamReader(req.Body).ReadToEndAsync();
                DataModelParameters parameters = JsonConvert.DeserializeObject<DataModelParameters>(stringBody);
                _logger.LogInformation($"CreateDatabase: Data connector is {parameters.DataConnector}");
                ResponseDto dsResponse = await _ds.GetDataSourceByNameAsync<ResponseDto>(parameters.DataConnector);
                if (dsResponse.IsSuccess) 
                {
                    ConnectParametersDto connector = JsonConvert.DeserializeObject<ConnectParametersDto>(Convert.ToString(dsResponse.Result));
                    _logger.LogInformation($"CreateDatabase: Connection string is {connector.ConnectionString}");
                    await _indexDB.CreateDatabaseIndex(connector.ConnectionString);
                
                }
                else
                {
                    _response.IsSuccess = false;
                    string newString = $"Createdatabase: Could not get data source {parameters.DataConnector}";
                    _response.ErrorMessages = dsResponse.ErrorMessages;
                    _response.ErrorMessages.Insert(0, newString);
                }
                
            }
            catch (Exception ex)
            {
                _response.IsSuccess = false;
                _response.ErrorMessages
                     = new List<string>() { ex.ToString() };
                _logger.LogError($"CreateDatabase: Error creating index database: {ex}");
            }
            return _response;
        }

        [Function("BuildIndex")]
        public async Task<ResponseDto> BuildIndex(
            [HttpTrigger(AuthorizationLevel.Function, "post", Route = "BuildIndex")] HttpRequestData req)
        {
            _logger.LogInformation("BuildIndex: Starting.");

            try
            {
                var stringBody = await new StreamReader(req.Body).ReadToEndAsync();
                BuildIndexParameters idxParms = JsonConvert.DeserializeObject<BuildIndexParameters>(stringBody);
                await _indexDB.BuildIndex(idxParms);
            }
            catch (Exception ex)
            {
                _response.IsSuccess = false;
                _response.ErrorMessages
                     = new List<string>() { ex.ToString() };
                _logger.LogError($"GetIndexes: Error getting indexes: {ex}");
            }
            _logger.LogInformation("BuildIndex: Completed.");
            return _response;
        }

        [Function("GetIndexes")]
        public async Task<ResponseDto> GetIndexes(
            [HttpTrigger(AuthorizationLevel.Function, "get", Route = "Indexes")] HttpRequestData req)
        {
            _logger.LogInformation("GetIndexes: Starting.");

            try
            {
                string name = req.GetQuery("Name", true);
                ResponseDto dsResponse = await _ds.GetDataSourceByNameAsync<ResponseDto>(name);
                ConnectParametersDto connectParameter = JsonConvert.DeserializeObject<ConnectParametersDto>(Convert.ToString(dsResponse.Result));
                IEnumerable<IndexDto> idx = await _indexDB.GetIndexes(connectParameter.ConnectionString);
                _response.Result = idx.ToList();
            }
            catch (Exception ex)
            {
                _response.IsSuccess = false;
                _response.ErrorMessages
                     = new List<string>() { ex.ToString() };
                _logger.LogError($"GetIndexes: Error getting indexes: {ex}");
            }
            return _response;
        }

        [Function("EntiretyIndexes")]
        public async Task<ResponseDto> GetEntiretyIndexes(
            [HttpTrigger(AuthorizationLevel.Function, "get", Route = "EntiretyIndexes")] HttpRequestData req)
        {
            _logger.LogInformation("EntiretyIndexes: Starting.");

            try
            {
                string name = req.GetQuery("Name", true);
                string dataType = req.GetQuery("DataType", true);
                string entiretyName = req.GetQuery("EntiretyName", true);
                string parentType = req.GetQuery("ParentType", true);
                ResponseDto dsResponse = await _ds.GetDataSourceByNameAsync<ResponseDto>(name);
                ConnectParametersDto connectParameter = JsonConvert.DeserializeObject<ConnectParametersDto>(Convert.ToString(dsResponse.Result));
                string sql = $"WITH Parents AS (SELECT * FROM pdo_qc_index WHERE DataType = '{parentType}')" +
                    $"SELECT A.IndexID FROM Parents A, pdo_qc_index B " +
                    $"WHERE B.IndexNode.IsDescendantOf(A.IndexNode) = 1 AND B.DATANAME = '{entiretyName}' AND B.DataType = '{dataType}'";
                IEnumerable<EntiretyListModel> idx = await _indexDB.GetEntiretyIndexes(sql, connectParameter.ConnectionString);
                _response.Result = idx.ToList();
            }
            catch (Exception ex)
            {
                _response.IsSuccess = false;
                _response.ErrorMessages
                     = new List<string>() { ex.ToString() };
                _logger.LogError($"GetIndexes: Error getting indexes: {ex}");
            }
            return _response;
        }

        [Function("QueryIndex")]
        public async Task<ResponseDto> QueryIndexes(
            [HttpTrigger(AuthorizationLevel.Function, "get", Route = "QueryIndex")] HttpRequestData req)
        {
            _logger.LogInformation("GetIndexes: Starting.");

            try
            {
                string name = req.GetQuery("Name", true);
                string dataType = req.GetQuery("DataType", false);
                string qcString = req.GetQuery("QcString", false);
                ResponseDto dsResponse = await _ds.GetDataSourceByNameAsync<ResponseDto>(name);
                ConnectParametersDto connectParameter = JsonConvert.DeserializeObject<ConnectParametersDto>(Convert.ToString(dsResponse.Result));
                IEnumerable<IndexDto> idx = await _indexDB.QueriedIndexes(connectParameter.ConnectionString, dataType, qcString);
                _response.Result = idx.ToList();
            }
            catch (Exception ex)
            {
                _response.IsSuccess = false;
                _response.ErrorMessages
                     = new List<string>() { ex.ToString() };
                _logger.LogError($"GetIndexes: Error getting indexes: {ex}");
            }
            return _response;
        }

        [Function("GetDmIndexes")]
        public async Task<ResponseDto> GetDmIndexes(
            [HttpTrigger(AuthorizationLevel.Function, "get", Route = "DmIndexes")] HttpRequestData req)
        {
            _logger.LogInformation("GetDmIndexes: Starting.");

            try
            {
                string name = req.GetQuery("Name", true);
                string indexNode = req.GetQuery("Node", false);
                if (string.IsNullOrEmpty(indexNode)) indexNode = "/";
                int? indexLevel = req.GetQuery("Level", false).GetIntFromString();
                if (!indexLevel.HasValue) indexLevel = 1;
                ResponseDto dsResponse = await _ds.GetDataSourceByNameAsync<ResponseDto>(name);
                ConnectParametersDto connectParameter = JsonConvert.DeserializeObject<ConnectParametersDto>(Convert.ToString(dsResponse.Result));
                // Wake up serverless database
                IEnumerable<DmIndexDto> idx = await _indexDB.GetDmIndexes(indexNode, (int)indexLevel, connectParameter.ConnectionString);
                _response.Result = idx.ToList();
            }
            catch (Exception ex)
            {
                _response.IsSuccess = false;
                _response.ErrorMessages
                     = new List<string>() { ex.ToString() };
                _logger.LogError($"GetDmIndexes: Error getting indexes: {ex}");
            }
            _logger.LogInformation("GetDmIndexex: Completed.");
            return _response;
        }

        [Function("GetDmIndex")]
        public async Task<ResponseDto> GetDmIndex(
            [HttpTrigger(AuthorizationLevel.Function, "get", Route = "DmIndex")] HttpRequestData req)
        {
            _logger.LogInformation("GetDmIndex: Starting.");

            try
            {
                string name = req.GetQuery("Name", true);
                int? id = req.GetQuery("Id", true).GetIntFromString();
                ResponseDto dsResponse = await _ds.GetDataSourceByNameAsync<ResponseDto>(name);
                ConnectParametersDto connectParameter = JsonConvert.DeserializeObject<ConnectParametersDto>(Convert.ToString(dsResponse.Result));
                IEnumerable<DmIndexDto> idx = await _indexDB.GetDmIndex((int)id, connectParameter.ConnectionString);
                _response.Result = idx.ToList();
            }
            catch (Exception ex)
            {
                _response.IsSuccess = false;
                _response.ErrorMessages
                     = new List<string>() { ex.ToString() };
                _logger.LogError($"GetDmIndex: Error getting indexes: {ex}");
            }
            _logger.LogInformation("GetDmIndex: Completed.");
            return _response;
        }


        [Function("GetDescendants")]
        public async Task<ResponseDto> GetDescendants(
            [HttpTrigger(AuthorizationLevel.Function, "get", Route = "GetDescendants/{id}")] HttpRequestData req, int id)
        {
            _logger.LogInformation("GetDescendants: Starting.");

            try
            {
                string name = req.GetQuery("Name", true);
                ResponseDto dsResponse = await _ds.GetDataSourceByNameAsync<ResponseDto>(name);
                if (dsResponse == null || !dsResponse.IsSuccess)
                {
                    _response.IsSuccess = false;
                    _response.ErrorMessages
                     = new List<string>() { $"GetDescendants: Could not get descendants with id {id}" };
                    _logger.LogError($"GetDescendants: Could not get descendants with id {id}");
                }
                ConnectParametersDto connectParameter = JsonConvert.DeserializeObject<ConnectParametersDto>(Convert.ToString(dsResponse.Result));
                IEnumerable<IndexDto> idx = await _indexDB.GetDescendants(id, "", connectParameter.ConnectionString);
                _response.Result = idx.ToList();
            }
            catch (Exception ex)
            {
                _response.IsSuccess = false;
                _response.ErrorMessages
                     = new List<string>() { ex.ToString() };
                _logger.LogError($"GetDescendants: Error getting indexes: {ex}");
            }
            _logger.LogInformation("GetDescendants: Completed.");
            return _response;
        }

        [Function("GetIndex")]
        public async Task<ResponseDto> GetIndex(
            [HttpTrigger(AuthorizationLevel.Function, "get", Route = "Indexes/{id}")] HttpRequestData req,
            int id)
        {
            _logger.LogInformation("GetIndex: Starting.");

            try
            {
                string name = req.GetQuery("Name", true);
                ResponseDto dsResponse = await _ds.GetDataSourceByNameAsync<ResponseDto>(name);
                ConnectParametersDto connectParameter = JsonConvert.DeserializeObject<ConnectParametersDto>(Convert.ToString(dsResponse.Result));
                IndexDto idx = await _indexDB.GetIndex(id, connectParameter.ConnectionString);
                _response.Result = idx;
            }
            catch (Exception ex)
            {
                _response.IsSuccess = false;
                _response.ErrorMessages
                     = new List<string>() { ex.ToString() };
                _logger.LogError($"GetIndexes: Error getting indexes: {ex}");
            }
            return _response;
        }

        [Function("SaveIndexes")]
        public async Task<ResponseDto> SaveIndexes(
            [HttpTrigger(AuthorizationLevel.Function, "post", Route = "Indexes")] HttpRequestData req)
        {
            _logger.LogInformation("SaveIndexes: Starting.");
            try
            {
                string name = req.GetQuery("Name", true);
                string dataType = req.GetQuery("Datatype", true);
                int? parentid = req.GetQuery("Parentid", true).GetIntFromString();
                ResponseDto dsResponse = await _ds.GetDataSourceByNameAsync<ResponseDto>(name);
                ConnectParametersDto connectParameter = JsonConvert.DeserializeObject<ConnectParametersDto>(Convert.ToString(dsResponse.Result));
                var stringBody = await new StreamReader(req.Body).ReadToEndAsync();
                JObject dataObject = JObject.Parse(stringBody);

                IndexDto rootIndex = await _indexDB.GetIndexRoot(connectParameter.ConnectionString);
                string jsonStringObject = rootIndex.JsonDataObject;
                IndexRootJson rootJson = JsonConvert.DeserializeObject<IndexRootJson>(jsonStringObject);
                _logger.LogInformation($"SaveIndexes: taxonomy is {rootJson.Taxonomy} ");
                DataAccessDef accessDef = rootJson.Source.GetDataAccessDefintionFromSourceJson(dataType);
                string taxonomy = rootJson.Taxonomy;
                _logger.LogInformation($"SaveIndexes: taxonomy is {taxonomy} ");
                JArray JsonIndexArray = JArray.Parse(taxonomy);
                List<IndexFileData> idxData = new List<IndexFileData>();
                foreach (JToken level in JsonIndexArray)
                {
                    idxData.Add(Common.ProcessJTokens(level));
                    idxData = Common.ProcessIndexArray(level, idxData);
                }
                IndexFileData taxonomyInfoForMissingObject = idxData.FirstOrDefault(x => x.DataName == dataType);
                string latitudeAttribute = taxonomyInfoForMissingObject.LatitudeAttribute;
                string longitudeAttribute = taxonomyInfoForMissingObject.LongitudeAttribute;

                double latitude = Common.GetLocationFromJson(dataObject, latitudeAttribute);
                double longitude = Common.GetLocationFromJson(dataObject, longitudeAttribute);
                IndexDto parentObject = await _indexDB.GetIndex((int)parentid, connectParameter.ConnectionString);
                if (taxonomyInfoForMissingObject.UseParentLocation)
                {
                    if (parentObject.Latitude != null) latitude = (double)parentObject.Latitude;
                    if (parentObject.Longitude != null) longitude = (double)parentObject.Longitude;
                }

                string dataName = dataObject[taxonomyInfoForMissingObject.NameAttribute].ToString();
                string dataKey = Common.GetDataKey(dataObject, accessDef.Keys);
                int nodeId = await GetIndextNode(dataType, parentObject, connectParameter.ConnectionString);
                IndexDto indexModel = new IndexDto();
                indexModel.Latitude = latitude;
                indexModel.Longitude = longitude;
                indexModel.DataType = dataType;
                indexModel.DataName = dataName;
                indexModel.DataKey = dataKey;
                indexModel.JsonDataObject = stringBody;
                _response.Result = await _indexDB.InsertIndex(indexModel, nodeId, connectParameter.ConnectionString);
            }
            catch (Exception ex)
            {
                _response.IsSuccess = false;
                _response.ErrorMessages
                     = new List<string>() { ex.ToString() };
                _logger.LogError($"SaveIndexes: Error saving index: {ex}");
            }
            return _response;
        }

        private async Task<int> GetIndextNode(string dataType, IndexDto idxResult, string connectionString)
        {
            int nodeid = 0;
            string nodeName = dataType + "s";
            if (idxResult != null)
            {
                IEnumerable<IndexDto> indexes = await _indexDB.GetDescendants(idxResult.IndexId, "", connectionString);
                if (indexes.Count() > 1)
                {
                    var nodeIndex = indexes.FirstOrDefault(x => x.DataType == nodeName);
                    if (nodeIndex != null)
                    {
                        nodeid = nodeIndex.IndexId;
                    }
                }
                if (nodeid == 0)
                {
                    IndexDto nodeIndex = new IndexDto();
                    nodeIndex.Latitude = 0.0;
                    nodeIndex.Longitude = 0.0;
                    nodeIndex.DataType = nodeName;
                    nodeIndex.DataName = nodeName;
                    nodeid = await _indexDB.InsertIndex(nodeIndex, idxResult.IndexId, connectionString);
                }
            }
            return nodeid;
        }

        [Function("UpdateIndexes")]
        public async Task<ResponseDto> UpdateIndexes(
            [HttpTrigger(AuthorizationLevel.Function, "put", Route = "Indexes")] HttpRequestData req)
        {
            _logger.LogInformation("UpdateIndexes: Starting");

            try
            {
                string name = req.GetQuery("Name", true);
                ResponseDto dsResponse = await _ds.GetDataSourceByNameAsync<ResponseDto>(name);
                ConnectParametersDto connectParameter = JsonConvert.DeserializeObject<ConnectParametersDto>(Convert.ToString(dsResponse.Result));
                var stringBody = await new StreamReader(req.Body).ReadToEndAsync();
                List<IndexDto> indexes = JsonConvert.DeserializeObject<List<IndexDto>>(Convert.ToString(stringBody));
                await _indexDB.UpdateIndexes(indexes, connectParameter.ConnectionString);
            }
            catch (Exception ex)
            {
                _response.IsSuccess = false;
                _response.ErrorMessages
                     = new List<string>() { ex.ToString() };
                _logger.LogError($"UpdateIndexes: Error updating indexes: {ex}");
            }

            _logger.LogInformation("UpdateIndexes: Completed");
            return _response;
        }

        [Function("DeleteIndex")]
        public async Task<ResponseDto> DeleteIndex(
            [HttpTrigger(AuthorizationLevel.Function, "delete", Route = "Indexes/{id}")] HttpRequestData req,
            int id)
        {
            _logger.LogInformation("DeleteIndex: Starting");
            try
            {
                string name = req.GetQuery("Name", true);
                ResponseDto dsResponse = await _ds.GetDataSourceByNameAsync<ResponseDto>(name);
                ConnectParametersDto connectParameter = JsonConvert.DeserializeObject<ConnectParametersDto>(Convert.ToString(dsResponse.Result));
                await _indexDB.DeleteIndex(id, connectParameter.ConnectionString);
            }
            catch (Exception ex)
            {
                _response.IsSuccess = false;
                _response.ErrorMessages
                     = new List<string>() { ex.ToString() };
                _logger.LogError($"UpdateIndexes: Error updating indexes: {ex}");
            }

            return _response;
        }
    }
}
