using Microsoft.Azure.Functions.Worker.Http;
using DatabaseManager.Services.Reports.Models;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Text;
using System.Security.Cryptography;
using Microsoft.VisualBasic.FileIO;

namespace DatabaseManager.Services.Reports.Extensions
{
    public static class CommonExtensions
    {
        public static string GetUniqKey(this string jsonData, RuleModel rule)
        {
            string keyText = "";
            string uniqKey = "";
            string[] keyAttributes = rule.RuleParameters.Split(';');
            if (!string.IsNullOrEmpty(jsonData))
            {
                JObject dataObject = JObject.Parse(jsonData);
                foreach (string key in keyAttributes)
                {
                    string function = "";
                    string normalizeParameter = "";
                    string attribute = key.Trim();
                    if (attribute.Substring(0, 1) == "*")
                    {
                        //attribute = attribute.Split('(', ')')[1];
                        int start = attribute.IndexOf("(") + 1;
                        int end = attribute.IndexOf(")", start);
                        function = attribute.Substring(0, start - 1);
                        string csv = attribute.Substring(start, end - start);
                        TextFieldParser parser = new TextFieldParser(new StringReader(csv));
                        parser.HasFieldsEnclosedInQuotes = true;
                        parser.SetDelimiters(",");
                        string[] parms = parser.ReadFields();
                        attribute = parms[0];
                        if (parms.Length > 1) normalizeParameter = parms[1];
                    }
                    string value = dataObject.GetValue(attribute).ToString();
                    if (function == "*NORMALIZE") value = value.NormalizeString(normalizeParameter);
                    if (function == "*NORMALIZE14") value = value.NormalizeString14();
                    keyText = keyText + value;
                    if (!string.IsNullOrEmpty(keyText)) uniqKey = keyText.GetSHA256Hash();
                }
            }
            return uniqKey;
        }

        public static string GetSHA256Hash(this string input)
        {
            try
            {
                using (SHA256 sha256 = SHA256.Create())
                {
                    byte[] inputBytes = Encoding.UTF8.GetBytes(input);
                    byte[] hashBytes = sha256.ComputeHash(inputBytes);
                    StringBuilder sb = new StringBuilder();
                    foreach (byte b in hashBytes)
                    {
                        sb.Append(b.ToString("x2"));
                    }
                    return sb.ToString();
                }
            }
            catch (Exception ex)
            {
                throw;
            }
        }

        public static string NormalizeString(this string str, string parms = "")
        {
            string[] charsToRemove = new string[] { "_", "-", "#", "*", ".", "@", "~", " ", "\t", "\n", "\r", "\r\n" };
            if (!string.IsNullOrEmpty(parms))
            {
                charsToRemove = parms.Select(x => x.ToString()).ToArray();
            }

            foreach (var c in charsToRemove)
            {
                str = str.Replace(c, string.Empty);
            }
            str = str.Replace("&", "AND");
            str = str.ToUpper();
            return str;
        }

        public static string NormalizeString14(this string str)
        {
            var charsToRemove = new string[] { "_", "-", "#", "*", ".", "@", "~", " ", "\t", "\n", "\r", "\r\n" };
            foreach (var c in charsToRemove)
            {
                str = str.Replace(c, string.Empty);
            }
            str = str.Replace("&", "AND");
            int length = str.Length;
            if (length < 14)
            {
                char pad = '0';
                str = str.PadRight(14, pad);
            }
            return str;
        }

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
        public static string ModifyJson(this string json, string path, string newValue)
        {
            JObject dataObject = JObject.Parse(json);
            dataObject[path] = newValue;
            string newJson = dataObject.ToString();
            return newJson;
        }
    }
}
