using Microsoft.Azure.Functions.Worker.Http;

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
    }
}
