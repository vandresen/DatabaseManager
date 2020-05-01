using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace DatabaseManager.Server.Entities
{
    public class DbKeys
    {
        private readonly Dictionary<string, string> _dictionary;

        public DbKeys()
        {
            _dictionary = new Dictionary<string, string>();
            _dictionary = new Dictionary<string, string>
                {
                    { "Rules", "Id" },
                    { "Index", "INDEXID" },
                    { "WellBore", "UWI" },
                    { "MarkerPick", "STRAT_NAME_SET_ID, STRAT_UNIT_ID, UWI" },
                    { "MarkerWell", "STRAT_NAME_SET_ID, STRAT_UNIT_ID, UWI" },
                    { "WellTop", "STRAT_NAME_SET_ID, STRAT_UNIT_ID" },
                    { "Log", "UWI, CURVE_ID"}
                };
        }

        public string this[string key]
        {
            get { return _dictionary[key]; }
        }
    }
}
