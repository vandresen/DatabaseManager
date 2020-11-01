using System;
using System.Collections.Generic;
using System.Text;

namespace DatabaseManager.Shared
{
    public class RuleTypeDictionary
    {
        public readonly Dictionary<string, string> _dictionary;

        public RuleTypeDictionary()
        {
            _dictionary = new Dictionary<string, string>
                {
                {"Entirety", "E" },
                {"Completeness", "C" },
                {"Consistency", "O" },
                {"Predictions", "P" },
                {"Uniqueness", "U" },
                {"Validity", "V" }
                };
        }

        public string this[string key]
        {
            get { return _dictionary[key]; }
        }
    }
}
