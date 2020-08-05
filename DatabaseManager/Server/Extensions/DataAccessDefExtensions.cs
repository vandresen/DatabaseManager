using DatabaseManager.Server.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace DatabaseManager.Server.Extensions
{
    public static class DataAccessDefExtensions
    {
        public static string GetSelectString(this List<DataAccessDef> dataAccessDefs, string dataType)
        {
            DataAccessDef dataDef = dataAccessDefs.First(x => x.DataType == dataType);
            string select = dataDef.Select;

            return select;
        }

        public static string GetKeysString(this List<DataAccessDef> dataAccessDefs, string dataType)
        {
            DataAccessDef dataDef = dataAccessDefs.First(x => x.DataType == dataType);
            string keys = dataDef.Keys;

            return keys;
        }
    }
}
