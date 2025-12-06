using EarthTool.PAR.GUI.ViewModels;
using EarthTool.PAR.Models.Abstracts;
using System.Collections.Generic;
using System.Reflection;
using EarthTool.PAR.Models;

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
  /// Creates property editors for all properties of a research.
  /// </summary>
  /// <param name="entity">The research to create editors for.</param>
  /// <returns>Collection of property editor ViewModels.</returns>
  IEnumerable<PropertyEditorViewModel> CreateEditorsForResearch(Research entity);
}
