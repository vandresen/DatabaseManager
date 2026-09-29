using Microsoft.Azure.Functions.Worker.Http;
using Newtonsoft.Json.Linq;
using System.Data;
using System.Globalization;

namespace DatabaseManager.Services.Predictions.Extensions
{
    public static class CommonExtensions
    {
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

        public static double[] ConvertStringToArray(this string input)
        {
            double[] output = new double[] { -99999.0 }; ;
            try
            {
                input = input.Trim();
                input = input.TrimStart('[').TrimEnd(']');
                string[] strOutputArray = input.Split(',')
                    .Select(x => x.Trim())
                    .Where(x => !string.IsNullOrWhiteSpace(x))
                    .ToArray();
                output = Array.ConvertAll(strOutputArray, Double.Parse);
            }
            catch (Exception)
            {
            }
            return output;
        }


        public static double? GetNumberFromJToken(this JToken token)
        {
            double? number = null;
            if (token != null)
            {
                string strNumber = token.ToString();
                if (!string.IsNullOrWhiteSpace(strNumber))
                {
                    double value;
                    if (double.TryParse(strNumber, out value)) number = value;
                }
            }
            return number;
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

        public static string GetTable(this string select)
        {
            select = select.ToUpper();
            int from = select.IndexOf(" FROM ") + 6;
            string table = select.Substring(from);
            return table;
        }

        public static T[] GetArrayFromString<T>(this string input) where T : IConvertible
        {
            if (string.IsNullOrWhiteSpace(input))
                return [];

            return input
                .Split(',')
                .Select(item =>
                {
                    item = item.Trim();
                    try
                    {
                        return (T)Convert.ChangeType(item, typeof(T), CultureInfo.InvariantCulture);
                    }
                    catch (Exception ex) when (ex is InvalidCastException or FormatException or OverflowException)
                    {
                        throw new InvalidOperationException(
                            $"Could not convert '{item}' to type {typeof(T).Name}.", ex);
                    }
                })
                .ToArray();
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

        public static void InitializeEnvironment(this HttpRequestData req)
        {
            string dbType = (req.GetQuery("DbType", false) ?? "sqlserver").Trim().ToLower();
            SD.Sqlite = dbType == "sqlite";
        }
    }
}
