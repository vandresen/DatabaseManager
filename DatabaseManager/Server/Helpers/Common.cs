using DatabaseManager.Server.Entities;
using DatabaseManager.Shared;
using Microsoft.Azure.Cosmos.Table;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Threading.Tasks;

namespace DatabaseManager.Server.Helpers
{
    public class Common
    {
        public static CloudTable GetTableConnect(string connectionString, string tableName)
        {
            CloudStorageAccount account = CloudStorageAccount.Parse(connectionString);
            CloudTableClient client = account.CreateCloudTableClient();
            CloudTable table = client.GetTableReference(tableName);
            return table;
        }

        public static ConnectParameters GetConnectParameters(string connectionString, string tableName, string name)
        {
            ConnectParameters connector = new ConnectParameters();
            CloudTable table = Common.GetTableConnect(connectionString, tableName);
            TableOperation retrieveOperation = TableOperation.Retrieve<SourceEntity>("PPDM", name);
            TableResult result = table.Execute(retrieveOperation);
            SourceEntity entity = result.Result as SourceEntity;
            if (entity == null)
            {
                connector = null;
            }
            else
            {
                connector.SourceName = name;
                connector.Database = entity.DatabaseName;
                connector.DatabaseServer = entity.DatabaseServer;
                connector.DatabaseUser = entity.User;
                connector.DatabasePassword = entity.Password;
                connector.ConnectionString = entity.ConnectionString;
            }
            return connector;
        }

        public static string ConvertDataRowToJson(DataRow dataRow, DataTable dt)
        {
            DataTable tmp = new DataTable();
            tmp = dt.Clone();
            tmp.Rows.Add(dataRow.ItemArray);
            string jsonData = JsonConvert.SerializeObject(tmp);
            jsonData = jsonData.Replace("[", "");
            jsonData = jsonData.Replace("]", "");
            return jsonData;
        }

        public static string FixAposInStrings(string st)
        {
            string fixString = st;
            int length;
            int start = 0;
            int end = st.IndexOf("'");
            while (end >= 0)
            {
                length = end;
                string s1 = fixString.Substring(0, length);
                string s2 = fixString.Substring(end);
                fixString = s1 + "'" + s2;
                start = end + 2;
                end = fixString.IndexOf("'", start);
            }
            return fixString;
        }

        public static string[] GetAttributes(string select)
        {
            int from = 7;
            int to = select.IndexOf("from");
            int length = to - 8;
            string attributes = select.Substring(from, length);
            string[] words = attributes.Split(',');

            return words;
        }

        public static string SetJsonDataObjectDate(string jsonText, string attribute)
        {
            string jsonDate = DateTime.Now.ToString("yyyy-MM-dd");
            JObject dataObject = JObject.Parse(jsonText);
            dataObject[attribute] = jsonDate;
            jsonText = dataObject.ToString();
            return jsonText;
        }
    }
}
