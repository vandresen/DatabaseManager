using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DatabaseManager.Services.DatabaseManagement.Initializer
{
    public interface IDbInitializer
    {
        public void InitializeInternalRuleFunctions(string connectionString);
        public void CreateDatabaseManagementTables(string connectionString);
    }
}
