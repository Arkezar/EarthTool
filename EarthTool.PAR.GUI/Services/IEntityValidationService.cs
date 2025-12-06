using EarthTool.PAR.Enums;
using EarthTool.PAR.GUI.Models;
using EarthTool.PAR.Models;
using EarthTool.PAR.Models.Abstracts;
using System.Collections.Generic;

namespace EarthTool.PAR.GUI.Services;

/// <summary>
/// Service for validating entities and their properties.
/// </summary>
public interface IEntityValidationService
{
  /// <summary>
  /// Sets the current PAR file context for reference validation.
  /// </summary>
  /// <param name="parFile">The current PAR file.</param>
  void SetContext(ParFile? parFile);

  /// <summary>
  /// Validates an entire entity.
  /// </summary>
  /// <param name="entity">The entity to validate.</param>
  /// <returns>Validation result.</returns>
  ValidationResult Validate(Entity entity);

  /// <summary>
  /// Validates a single property value.
  /// </summary>
  /// <param name="entity">The entity being validated.</param>
  /// <param name="propertyName">Name of the property.</param>
  /// <param name="value">The value to validate.</param>
  /// <returns>Validation result.</returns>
  ValidationResult ValidateProperty(Entity entity, string propertyName, object? value);

  /// <summary>
  /// Validates that an entity reference exists.
  /// </summary>
  /// <param name="referenceId">The reference ID to validate.</param>
  /// <param name="groupType">Optional group type to narrow search.</param>
  /// <returns>True if the reference exists.</returns>
  bool ValidateEntityReference(string referenceId, EntityGroupType? groupType = null);

  /// <summary>
  /// Gets all available entity references of a specific group type.
  /// </summary>
  /// <param name="groupType">The group type to filter by.</param>
  /// <returns>List of entity names.</returns>
  IEnumerable<string> GetAvailableReferences(EntityGroupType? groupType = null);
}
