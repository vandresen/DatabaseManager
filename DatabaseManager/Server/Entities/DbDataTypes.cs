using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace DatabaseManager.Server.Entities
{
    public class DbDataTypes
    {
        private readonly string[] _datatypes;
        public string[] DataTypes { get; private set; }

        public DbDataTypes()
        {
            _datatypes = new string[]
            {
                "Rules",
                "Functions",
                "WellBore",
                "MarkerPick",
                "WellTop",
                "Log",
                "LogCurve"
            };
            DataTypes = _datatypes;
        }
    }
}
