using EarthTool.PAR.Enums;
using EarthTool.PAR.Models;
using EarthTool.PAR.Models.Abstracts;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Reflection;
using ValidationResult = EarthTool.PAR.GUI.Models.ValidationResult;
using ValidationError = EarthTool.PAR.GUI.Models.ValidationError;
using ValidationSeverity = EarthTool.PAR.GUI.Models.ValidationSeverity;

namespace EarthTool.PAR.GUI.Services;

/// <summary>
/// Implementation of entity validation service.
/// </summary>
public class EntityValidationService : IEntityValidationService
{
  private readonly ILogger<EntityValidationService> _logger;
  private ParFile? _currentParFile;

  public EntityValidationService(ILogger<EntityValidationService> logger)
  {
    _logger = logger ?? throw new ArgumentNullException(nameof(logger));
  }

  /// <inheritdoc/>
  public void SetContext(ParFile? parFile)
  {
    _currentParFile = parFile;
    _logger.LogDebug("Validation context updated");
  }

  /// <inheritdoc/>
  public ValidationResult Validate(Entity entity)
  {
    if (entity == null)
      throw new ArgumentNullException(nameof(entity));

    var result = new ValidationResult();

    // 1. Validate basic properties
    ValidateBasicProperties(entity, result);

    // 2. Validate entity references
    ValidateReferences(entity, result);

    // 3. Validate research IDs
    ValidateResearch(entity, result);

    _logger.LogDebug("Validated entity '{Name}': {ErrorCount} errors, {WarningCount} warnings",
      entity.Name,
      result.Errors.Count(e => e.Severity == ValidationSeverity.Error),
      result.Errors.Count(e => e.Severity == ValidationSeverity.Warning));

    return result;
  }

  /// <inheritdoc/>
  public ValidationResult ValidateProperty(Entity entity, string propertyName, object? value)
  {
    if (entity == null)
      throw new ArgumentNullException(nameof(entity));

    if (string.IsNullOrEmpty(propertyName))
      throw new ArgumentException("Property name cannot be null or empty", nameof(propertyName));

    var result = new ValidationResult();
    var property = entity.GetType().GetProperty(propertyName);

    if (property == null)
    {
      result.Errors.Add(new ValidationError
      {
        PropertyName = propertyName,
        ErrorMessage = $"Property '{propertyName}' not found",
        Severity = ValidationSeverity.Error
      });
      return result;
    }

    // Check required attribute
    if (property.GetCustomAttribute<RequiredAttribute>() != null && value == null)
    {
      result.Errors.Add(new ValidationError
      {
        PropertyName = propertyName,
        ErrorMessage = $"{propertyName} is required",
        Severity = ValidationSeverity.Error
      });
    }

    // Check range for int
    if (property.PropertyType == typeof(int) && value != null)
    {
      var rangeAttr = property.GetCustomAttribute<RangeAttribute>();
      if (rangeAttr != null)
      {
        int intValue = (int)value;
        if (intValue < (int)rangeAttr.Minimum || intValue > (int)rangeAttr.Maximum)
        {
          result.Errors.Add(new ValidationError
          {
            PropertyName = propertyName,
            ErrorMessage = $"{propertyName} must be between {rangeAttr.Minimum} and {rangeAttr.Maximum}",
            Severity = ValidationSeverity.Error
          });
        }
      }
    }

    // Check max length for string
    if (property.PropertyType == typeof(string) && value != null)
    {
      var maxLengthAttr = property.GetCustomAttribute<MaxLengthAttribute>();
      if (maxLengthAttr != null)
      {
        string strValue = (string)value;
        if (strValue.Length > maxLengthAttr.Length)
        {
          result.Errors.Add(new ValidationError
          {
            PropertyName = propertyName,
            ErrorMessage = $"{propertyName} cannot exceed {maxLengthAttr.Length} characters",
            Severity = ValidationSeverity.Error
          });
        }
      }
    }

    return result;
  }

  /// <inheritdoc/>
  public bool ValidateEntityReference(string referenceId, EntityGroupType? groupType = null)
  {
    if (string.IsNullOrEmpty(referenceId))
      return false;

    if (_currentParFile == null)
      return false;

    var query = _currentParFile.Groups.SelectMany(g => g.Entities);

    if (groupType.HasValue)
    {
      query = _currentParFile.Groups
        .Where(g => g.GroupType == groupType.Value)
        .SelectMany(g => g.Entities);
    }

    return query.Any(e => e.Name.Equals(referenceId, StringComparison.OrdinalIgnoreCase));
  }

  /// <inheritdoc/>
  public IEnumerable<string> GetAvailableReferences(EntityGroupType? groupType = null)
  {
    if (_currentParFile == null)
      return Enumerable.Empty<string>();

    var query = _currentParFile.Groups.SelectMany(g => g.Entities);

    if (groupType.HasValue)
    {
      query = _currentParFile.Groups
        .Where(g => g.GroupType == groupType.Value)
        .SelectMany(g => g.Entities);
    }

    return query.Select(e => e.Name).OrderBy(n => n);
  }

  private void ValidateBasicProperties(Entity entity, ValidationResult result)
  {
    // Validate Name
    if (string.IsNullOrWhiteSpace(entity.Name))
    {
      result.Errors.Add(new ValidationError
      {
        PropertyName = "Name",
        ErrorMessage = "Entity name is required",
        Severity = ValidationSeverity.Error
      });
    }
  }

  private void ValidateReferences(Entity entity, ValidationResult result)
  {
    if (_currentParFile == null)
      return;

    // Get all properties ending with "Id" that are strings
    var referenceProperties = entity.GetType()
      .GetProperties()
      .Where(p => p.Name.EndsWith("Id") && p.PropertyType == typeof(string));

    foreach (var prop in referenceProperties)
    {
      var refId = (string?)prop.GetValue(entity);
      if (string.IsNullOrEmpty(refId))
        continue;

      // Check if referenced entity exists
      if (!ValidateEntityReference(refId))
      {
        result.Errors.Add(new ValidationError
        {
          PropertyName = prop.Name,
          ErrorMessage = $"Referenced entity '{refId}' not found",
          Severity = ValidationSeverity.Warning
        });
      }
    }
  }

  private void ValidateResearch(Entity entity, ValidationResult result)
  {
    if (_currentParFile == null || entity.RequiredResearch == null)
      return;

    // For now, just warn if the list is too long
    if (entity.RequiredResearch.Count() > 20)
    {
      result.Errors.Add(new ValidationError
      {
        PropertyName = "RequiredResearch",
        ErrorMessage = $"Entity has {entity.RequiredResearch.Count()} research requirements (unusually high)",
        Severity = ValidationSeverity.Warning
      });
    }
  }
}
