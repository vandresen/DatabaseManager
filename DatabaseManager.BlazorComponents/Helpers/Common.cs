using DatabaseManager.Shared;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DatabaseManager.BlazorComponents.Helpers
{
    public class Common
    {
        public static string CreateDatabaseConnectionString(ConnectParameters connection)
        {
            string source = $"Source={connection.DatabaseServer};";
            string database = $"Initial Catalog ={connection.Catalog};";
            string timeout = "Connection Timeout=120";
            string persistSecurity = "Persist Security Info=False;";
            string multipleActive = "MultipleActiveResultSets=True;";
            string integratedSecurity = "";
            string user = "";
            //Encryption is currently not used, more testing later
            //string encryption = "Encrypt=True;TrustServerCertificate=False;";
            string encryption = "Encrypt=False;";
            if (!string.IsNullOrWhiteSpace(connection.User))
                user = $"User ID={connection.User};";
            else
                integratedSecurity = "Integrated Security=True;";
            string password = "";
            if (!string.IsNullOrWhiteSpace(connection.Password)) password = $"Password={connection.Password};";

            string cnStr = "Data " + source + persistSecurity + database +
                user + password + integratedSecurity + encryption + multipleActive;

            cnStr = cnStr + timeout;

            return cnStr;
        }
    }
}
