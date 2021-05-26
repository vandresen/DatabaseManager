using DatabaseManager.Components;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DatabaseManager.LocalDataTransfer
{
    public class DataTransfer
    {
        private DbUtilities _dbConn;

        public DataTransfer()
        {
            _dbConn = new DbUtilities();
        }

        public void DeleteTables()
        {

        }
    }
}
