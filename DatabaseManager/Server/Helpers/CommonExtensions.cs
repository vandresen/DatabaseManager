using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DatabaseManager.Server.Helpers
{
    public static class CommonExtensions
    {
        public static string EncodeBase64(this string value)
        {
            var valueBytes = Encoding.UTF8.GetBytes(value);
            return Convert.ToBase64String(valueBytes);
        }

        public static string DecodeBase64(this string value)
        {
            var valueBytes = System.Convert.FromBase64String(value);
            return Encoding.UTF8.GetString(valueBytes);
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

        public static long CountLines(this string text)
        {
            var lineCounter = 0L;
            for (int i = 0; i < text.Length; i++)
            {
                if (text[i] == '\n') lineCounter++;
            }

            return lineCounter;
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

        public static string NormalizeString(this string str)
        {
            var charsToRemove = new string[] { "_", "-", "#", "*", ".", "@", "~" };
            foreach (var c in charsToRemove)
            {
                str = str.Replace(c, string.Empty);
            }
            return str;
        }
    }
}
