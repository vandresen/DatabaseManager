using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DatabaseManager.Common.DBAccess
{
    public interface IDatabaseAccess
    {
        void WakeUpDatabase(string connectionString);
    }
}
