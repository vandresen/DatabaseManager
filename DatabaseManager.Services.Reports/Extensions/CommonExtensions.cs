using Microsoft.Azure.Functions.Worker.Http;
using DatabaseManager.Services.Reports.Models;
using Newtonsoft.Json;

namespace DatabaseManager.Services.Reports.Extensions
{
    public static class CommonExtensions
    {
        public static string BuildFunctionUrl(this string url, string function, string query, string apiKey)
        {
            bool buildQuery = false;
            url = url + function;
            if (!string.IsNullOrEmpty(query)) buildQuery = true;
            if (!string.IsNullOrEmpty(apiKey)) buildQuery = true;
            if (buildQuery) url = url + "?";
            if (!string.IsNullOrEmpty(query)) url = url + query + "&";
            if (!string.IsNullOrEmpty(apiKey)) url = url + "code=" + apiKey;
            if (url.EndsWith("&")) url = url.Substring(0, url.Length - 1);
            return url;
        }

        public static string GetQuery(this HttpRequestData req, string queryAttribute, bool mandatory)
        {
            var query = System.Web.HttpUtility.ParseQueryString(req.Url.Query);
            string result = query[queryAttribute];
            if (string.IsNullOrEmpty(result) && mandatory)
            {
                Exception error = new Exception($"Error getting query result for {queryAttribute}");
                throw error;
            }
            return result;
        }

        public static string GetTable(this string select)
        {
            select = select.ToUpper();
            int from = select.IndexOf(" FROM ") + 6;
            string table = select.Substring(from);
            return table;
        }

        public static int? GetIntFromString(this string token)
        {
            int? number = null;
            if (!string.IsNullOrWhiteSpace(token))
            {
                int value;
                if (int.TryParse(token, out value)) number = value;
            }
            return number;
        }

        public static DataAccessDef GetDataAccessDefintionFromSourceJson(this string dataConnectorJson, string dataType)
        {
            ConnectParametersDto source = JsonConvert.DeserializeObject<ConnectParametersDto>(dataConnectorJson);
            List<DataAccessDef> accessDefs = JsonConvert.DeserializeObject<List<DataAccessDef>>(source.DataAccessDefinition);
            DataAccessDef accessDef = accessDefs.FirstOrDefault(x => x.DataType == dataType)
                ?? throw new InvalidOperationException($"No access definition found for data type {dataType}"); ;
            return accessDef;
        }

        public static string GetConnectionStringFromSourceJson(this string dataConnectorJson)
        {
            ConnectParametersDto source = JsonConvert.DeserializeObject<ConnectParametersDto>(dataConnectorJson);
            return source.ConnectionString;
        }

        public static string[] GetAttributes(this string select)
        {
            int from = 7;
            int to = select.IndexOf("from");
            int length = to - 8;
            string attributes = select.Substring(from, length);
            string[] words = attributes.Split(',')
                .Select(item => item.Trim())
                .ToArray();
            return words;
        }

        public static Dictionary<string, string> ToStringDictionary(this string stringData, char propertyDelimiter = ';', char keyValueDelimiter = '=')
        {
            Dictionary<string, string> keyValuePairs = new Dictionary<string, string>();
            if (!string.IsNullOrEmpty(stringData))
            {
                Array.ForEach<string>(stringData.Split(propertyDelimiter), s =>
                {
                    string key = s.Split(keyValueDelimiter)[0].Trim();
                    string value = s.Split(keyValueDelimiter)[1];
                    keyValuePairs.Add(key, value);
                });
            }

            return keyValuePairs;
        }
    }
}
