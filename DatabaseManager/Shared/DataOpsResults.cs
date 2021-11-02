using System;
using System.Collections.Generic;
using System.Text;

namespace DatabaseManager.Shared
{
    public class DataOpsResults
    {
        public string statusQueryGetUri { get; set; }
        public string sendEventPostUri { get; set; }
        public string terminatePostUri { get; set; }
        public string purgeHistoryDeleteUri { get; set; }
    }
}
