using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DatabaseManager.Services.DataTransfer.Models
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
        "R_WELL_DATUM_TYPE",
        "R_WELL_STATUS"
        };
    }
}
