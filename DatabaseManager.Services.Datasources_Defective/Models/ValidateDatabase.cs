using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DatabaseManager.Services.Datasources.Models
{
    class ValidateDatabase : ValidationAttribute
    {
        protected override ValidationResult IsValid(object value, ValidationContext validationContext)
        {
            ConnectParameters connector = (ConnectParameters)validationContext.ObjectInstance;
            if (connector.SourceType == "DataBase")
            {
                if (value == null) return new ValidationResult("Attribute is required.", new[] { validationContext.MemberName });
            }
            return ValidationResult.Success;
        }
    }
}
