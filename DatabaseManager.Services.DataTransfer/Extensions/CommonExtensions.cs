using Microsoft.Azure.Functions.Worker.Http;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Security.Cryptography;
using DatabaseManager.Services.DataTransfer.Models;

namespace DatabaseManager.Services.DataTransfer.Extensions
{
    public static class CommonExtensions
    {
        public static string GetStorageKey(this HttpRequestData req)
        {
            var headers = req.Headers;
            IEnumerable<string> headerSerachResult;
            string storageAccount = string.Empty;
            if (headers.TryGetValues("azurestorageconnection", out headerSerachResult))
            {
                storageAccount = headerSerachResult.First();
            }
            if (string.IsNullOrEmpty(storageAccount))
            {
                Exception error = new Exception($"Error getting azure storage key");
                throw error;
            }
            return storageAccount;
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

        public static string Truncate(this string value, int maxLength)
        {
            return value?.Substring(0, Math.Min(value.Length, maxLength));
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

        public static string FixAposInStrings(this string st)
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

        public static string SetJsonDataObjectDate(this string jsonText, string attribute)
        {
            string jsonDate = DateTime.Now.ToString("yyyy-MM-dd");
            JObject dataObject = JObject.Parse(jsonText);
            dataObject[attribute] = jsonDate;
            jsonText = dataObject.ToString();
            return jsonText;
        }

        public static Dictionary<string, int> ToDictionary(this string stringData, char propertyDelimiter = ';', char keyValueDelimiter = '=')
        {
            Dictionary<string, int> keyValuePairs = new Dictionary<string, int>();
            Array.ForEach<string>(stringData.Split(propertyDelimiter), s =>
            {
                string name = s.Split(keyValueDelimiter)[0].Trim();
                int column = Convert.ToInt32(s.Split(keyValueDelimiter)[1]);
                keyValuePairs.Add(name, column);
            });

            return keyValuePairs;
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

        public static Dictionary<string, string> GetColumnTypes(this DataTable dt)
        {
            Dictionary<string, string> keyValuePairs = new Dictionary<string, string>();
            DataRow tmpRow = dt.NewRow();
            var count = tmpRow.Table.Columns.Count;
            for (int i = 0; i < count; i++)
            {
                string name = dt.Columns[i].ColumnName.ToString();
                string type = dt.Columns[i].DataType.ToString();
                keyValuePairs.Add(name, type);
            }
            return keyValuePairs;
        }

        public static string GetSHA256Hash(this string str)
        {
            //StreamWriter sw = null;
            try
            {
                using (SHA256 sha256 = SHA256.Create())
                {
                    byte[] inputBytes = Encoding.UTF8.GetBytes(str);
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

        public static string GetDatabaseAttributeType(this TableSchema attributeProperties)
        {
            string attributeType = attributeProperties.TYPE_NAME;
            if (string.IsNullOrEmpty(attributeType)) attributeType = attributeProperties.DATA_TYPE;
            if (attributeType == "nvarchar")
            {
                string charLength = attributeProperties.PRECISION;
                attributeType = attributeType + "(" + charLength + ")";
            }
            if (attributeType == "int identity")
            {
                attributeType = "int";
            }
            else if (attributeType == "numeric")
            {
                string numericPrecision = attributeProperties.PRECISION;
                string numericScale = attributeProperties.SCALE;
                attributeType = attributeType + "(" + numericPrecision + "," + numericScale + ")";
            }
            return attributeType;
        }

        public static string[] GetSqlSelectAttributes(this string sql)
        {
            string sqlSelect = sql.Trim().TrimEnd(';');

            // Extract the substring between "SELECT" and "FROM" keywords
            int selectIndex = sqlSelect.IndexOf("SELECT", StringComparison.OrdinalIgnoreCase);
            int fromIndex = sqlSelect.IndexOf("FROM", StringComparison.OrdinalIgnoreCase);
            if (selectIndex == -1 || fromIndex == -1)
            {
                throw new ArgumentException("Invalid SQL SELECT statement.");
            }

            string selectSubstring = sqlSelect.Substring(selectIndex + 6, fromIndex - selectIndex - 6).Trim();

            // Split the substring by commas and return the resulting array
            return selectSubstring.Split(',')
                .Select(attr => attr.Trim())
                .ToArray();
        }
    }
}
