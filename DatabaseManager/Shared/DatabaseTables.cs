using System;
using System.Collections.Generic;
using System.Text;

namespace DatabaseManager.Shared
{
    public class DatabaseTables
    {
        public static readonly IList<string> Names = new string[]
        {
        "WELL_LOG_CURVE_VALUE",
        "WELL_LOG_CURVE",
        "STRAT_WELL_SECTION",
        "STRAT_UNIT",
        "STRAT_NAME_SET",
        "WELL",
        "BUSINESS_ASSOCIATE",
        "FIELD",
        "R_WELL_DATUM_TYPE",
        "R_WELL_STATUS"
        };
    }
}
