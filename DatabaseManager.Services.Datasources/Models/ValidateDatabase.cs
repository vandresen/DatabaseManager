﻿using System.ComponentModel.DataAnnotations;

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
