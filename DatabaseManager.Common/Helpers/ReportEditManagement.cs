using AutoMapper;
using DatabaseManager.Common.Data;
using DatabaseManager.Common.DBAccess;
using DatabaseManager.Common.Entities;
using DatabaseManager.Common.Extensions;
using DatabaseManager.Common.Services;
using DatabaseManager.Shared;
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DatabaseManager.Common.Helpers
{
    public class ReportEditManagement
    {
        private readonly string azureConnectionString;
        private readonly DapperDataAccess _dp;
        private readonly IFileStorageServiceCommon _fileStorage;
        private readonly IIndexDBAccess _indexData;

        public ReportEditManagement()
        {
            _dp = new DapperDataAccess();
            _indexData = new IndexDBAccess(_dp);
        }

        public ReportEditManagement(string azureConnectionString)
        {
            _dp = new DapperDataAccess();
            this.azureConnectionString = azureConnectionString;
            var builder = new ConfigurationBuilder();
            IConfiguration configuration = builder.Build();
            _fileStorage = new AzureFileStorageServiceCommon(configuration);
            _fileStorage.SetConnectionString(azureConnectionString);
            _dp = new DapperDataAccess();
            _indexData = new IndexDBAccess(_dp);
        }

        public async Task<string> GetAttributeInfo(string sourceName, string dataType)
        {
            string json = "";
            ConnectParameters connector = await Common.GetConnectParameters(azureConnectionString, sourceName);

            string accessJson = await _fileStorage.ReadFile("connectdefinition", "PPDMDataAccess.json");
            List<DataAccessDef> accessDefs = JsonConvert.DeserializeObject<List<DataAccessDef>>(accessJson);
            DataAccessDef dataAccess = accessDefs.First(x => x.DataType == dataType);
            string sql = dataAccess.Select;
            string table = Common.GetTable(sql);
            string query = $" where 0 = 1";

            DbUtilities dbConn = new DbUtilities();
            dbConn.OpenWithConnectionString(connector.ConnectionString);
            DataTable dt = dbConn.GetDataTable(sql, query);
            AttributeInfo attributeInfo = new AttributeInfo();
            attributeInfo.DataAttributes = dt.GetColumnTypes();
            json  = JsonConvert.SerializeObject(attributeInfo);

            return json;
        }

        public async Task InsertEdits(ReportData reportData, string sourceName)
        {
            ConnectParameters connector = await Common.GetConnectParameters(azureConnectionString, sourceName);
            IndexModel index = await _indexData.GetIndexFromSP(reportData.Id, connector.ConnectionString);
            JObject dataObject = JObject.Parse(reportData.JsonData);

            if (reportData.ColumnType == "number")
            {
                dataObject[reportData.ColumnName] = reportData.NumberValue;
            }
            else if (reportData.ColumnType == "string")
            {
                dataObject[reportData.ColumnName] = reportData.TextValue;
            }
            string remark = dataObject["REMARK"] + $";{reportData.ColumnName} has been manually edited;";
            dataObject["REMARK"] = remark;
            index.JsonDataObject = dataObject.ToString();
            string failRule = reportData.RuleKey + ";";
            index.QC_String = index.QC_String.Replace(failRule, "");
            await _indexData.UpdateIndex(index, connector.ConnectionString);
            UpdateDatabase(index.JsonDataObject, connector.ConnectionString, reportData.DataType);
        }

        private void UpdateDatabase(string jsonDataObject, string connectionString, string dataType)
        {
            JObject dataObject = JObject.Parse(jsonDataObject);
            dataObject["ROW_CHANGED_BY"] = Environment.UserName;
            jsonDataObject = dataObject.ToString();
            jsonDataObject = Helpers.Common.SetJsonDataObjectDate(jsonDataObject, "ROW_CHANGED_DATE");
            DbUtilities dbConn = new DbUtilities();
            dbConn.OpenWithConnectionString(connectionString);
            dbConn.UpdateDataObject(jsonDataObject, dataType);
            dbConn.CloseConnection();
        }
    }
}
