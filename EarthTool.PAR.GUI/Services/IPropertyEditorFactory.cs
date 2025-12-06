using EarthTool.PAR.GUI.ViewModels;
using EarthTool.PAR.Models.Abstracts;
using System.Collections.Generic;
using System.Reflection;

namespace EarthTool.PAR.GUI.Services;

/// <summary>
/// Factory for creating property editor ViewModels.
/// </summary>
public interface IPropertyEditorFactory
{
  /// <summary>
  /// Creates property editors for all properties of an entity.
  /// </summary>
  /// <param name="entity">The entity to create editors for.</param>
  /// <returns>Collection of property editor ViewModels.</returns>
  IEnumerable<PropertyEditorViewModel> CreateEditorsForEntity(Entity entity);

  /// <summary>
  /// Creates a property editor for a specific property.
  /// </summary>
  /// <param name="entity">The entity containing the property.</param>
  /// <param name="property">The property info.</param>
  /// <returns>Property editor ViewModel.</returns>
  PropertyEditorViewModel? CreateEditorForProperty(Entity entity, PropertyInfo property);
}
