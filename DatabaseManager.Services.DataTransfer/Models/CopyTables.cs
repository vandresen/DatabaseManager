using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DatabaseManager.Services.DataTransfer.Models
{
    public class CopyTables
    {
        public static readonly Dictionary<string, string> dictionary = new Dictionary<string, string>
        {
            { "WELL", "TABLE" },
            { "BUSINESS_ASSOCIATE", "REFERENCE" },
            { "FIELD", "REFERENCE" },
            { "R_WELL_DATUM_TYPE", "REFERENCE" },
            { "R_WELL_STATUS", "REFERENCE" },
            { "STRAT_NAME_SET", "REFERENCE" },
            { "STRAT_UNIT", "REFERENCE"},
            { "STRAT_WELL_SECTION", "TABLE"},
            { "WELL_LOG_CURVE", "TABLE"},
            { "WELL_LOG_CURVE_VALUE", "TABLE"}
        };
    }
}
