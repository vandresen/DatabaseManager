﻿using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Threading.Tasks;

namespace DatabaseManager.Server.Helpers
{
    public static class CommonExtensions
    {
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
    }
}