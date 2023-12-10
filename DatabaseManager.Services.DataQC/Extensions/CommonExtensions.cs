using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DatabaseManager.Services.DataQC.Extensions
{
    public static class CommonExtensions
    {
        public static string ConsistencyCheck(this string strValue, string strRefValue, string valueType)
        {
            string status = "Passed";
            if (valueType == "System.Decimal")
            {
                double number;
                double refNumber;
                Boolean isNumber = double.TryParse(strValue, out number);
                if (isNumber)
                {
                    isNumber = double.TryParse(strRefValue, out refNumber);
                    if (isNumber)
                    {
                        if (Math.Abs(refNumber - number) > 0.0000001) status = "Failed";
                    }
                }
            }
            else
            {
                if (strValue != strRefValue) status = "Failed";
            }
            return status;
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

        public static string CompletenessCheck(this string strValue)
        {
            string status = "Passed";
            if (string.IsNullOrWhiteSpace(strValue))
            {
                status = "Failed";
            }
            else
            {
                double number;
                bool canConvert = double.TryParse(strValue, out number);
                if (canConvert)
                {
                    if (number == -99999) status = "Failed";
                }
            }
            return status;
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
    }
}
