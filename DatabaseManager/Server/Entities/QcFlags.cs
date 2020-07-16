using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace DatabaseManager.Server.Entities
{
    public class QcFlags
    {
        private readonly Dictionary<Int32, string> _dictionary;

        public QcFlags()
        {
            _dictionary = new Dictionary<Int32, string>();
        }

        public string this[Int32 key]
        {
            get { return _dictionary[key]; }
            set { _dictionary[key] = value; }
        }
    }
}
