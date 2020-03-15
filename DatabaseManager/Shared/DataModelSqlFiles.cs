using System;
using System.Collections.Generic;
using System.Text;

namespace DatabaseManager.Shared
{
    public class DataModelSqlFiles
    {
        public static readonly IList<string> Names = new string[]
        {
            "TAB.sql",
            "PK.sql",
            "CK.sql",
            "FK.sql"
        };
    }
}
