using System;
using System.Collections.Generic;
using System.Text;

namespace DatabaseManager.Common.Entities
{
    public class ColumnProperties
    {
        private readonly Dictionary<string, string> _dictionary;

        public ColumnProperties()
        {
            _dictionary = new Dictionary<string, string>();
        }

        public string this[string key]
        {
            get { return _dictionary[key]; }
            set { _dictionary[key] = value; }
        }
    }
}
