using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DatabaseManager.Common.DBAccess
{
    public class ADODataAccess
    {
        private readonly string _connectionString;

        public ADODataAccess(string connectionString)
        {
            _connectionString = connectionString;
        }
    }
}
