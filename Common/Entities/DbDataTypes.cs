using System;
using System.Collections.Generic;
using System.Text;

namespace DatabaseManager.Common.Entities
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
