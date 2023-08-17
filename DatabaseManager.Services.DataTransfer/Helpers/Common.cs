using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DatabaseManager.Services.DataTransfer.Helpers
{
    public class Common
    {
        public static string GetSqlDbTypeString(Type dataType)
        {
            if (dataType == typeof(string))
                return "NVARCHAR(100)"; // Adjust the length as needed
            if (dataType == typeof(int))
                return "INT";
            if (dataType == typeof(double))
                return "FLOAT";
            if (dataType == typeof(bool))
                return "BIT";

            return "NVARCHAR(100)"; // Default to NVARCHAR if type is not recognized
        }
    }
}
