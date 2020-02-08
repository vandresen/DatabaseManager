using System;
using System.Collections.Generic;
using System.Text;

namespace DatabaseManager.Shared
{
    public class ConnectParameters
    {
        public string Database { get; set; }

        public string DatabaseServer { get; set; }

        public string DatabaseUser { get; set; }

        public string DatabasePassword { get; set; }
    }
}
