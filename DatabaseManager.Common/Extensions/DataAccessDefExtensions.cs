using DatabaseManager.Common.Entities;
using DatabaseManager.Shared;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DatabaseManager.Common.Extensions
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

        public static DataAccessDef GetDataAccessDefintionFromSourceJson(this string dataConnectorJson, string dataType)
        {
            ConnectParameters source = JsonConvert.DeserializeObject<ConnectParameters>(dataConnectorJson);
            List<DataAccessDef> accessDefs = JsonConvert.DeserializeObject<List<DataAccessDef>>(source.DataAccessDefinition);
            DataAccessDef accessDef = accessDefs.First(x => x.DataType == dataType);
            return accessDef;
        }
    }
}
