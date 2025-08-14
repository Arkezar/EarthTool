using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;

namespace EarthTool.PAR.GUI.Services;

public class ValidationService
{
  public ValidationResult ValidateProperty(object value, string propertyName, Type objectType)
  {
    var property = objectType.GetProperty(propertyName);
    if (property == null)
    {
      return new ValidationResult($"Property {propertyName} not found");
    }

    var validationAttributes = property.GetCustomAttributes(typeof(ValidationAttribute), true)
      .Cast<ValidationAttribute>();

    var validationContext = new ValidationContext(new object())
    {
      MemberName = propertyName
    };

    var results = new List<ValidationResult>();
    Validator.TryValidateValue(value, validationContext, results, validationAttributes);

    return results.FirstOrDefault() ?? ValidationResult.Success!;
  }

  public bool IsValidInteger(string input, int minValue = int.MinValue, int maxValue = int.MaxValue)
  {
    if (int.TryParse(input, out int value))
    {
      return value >= minValue && value <= maxValue;
    }
    return false;
  }

  public bool IsValidString(string input, int maxLength = 255, bool allowEmpty = true)
  {
    if (!allowEmpty && string.IsNullOrWhiteSpace(input))
    {
      return false;
    }

    return input?.Length <= maxLength;
  }

  public bool IsValidEntityId(string input)
  {
    return !string.IsNullOrWhiteSpace(input) && input.Length <= 100;
  }
}