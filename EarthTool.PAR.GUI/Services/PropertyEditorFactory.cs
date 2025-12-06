using EarthTool.PAR.GUI.ViewModels;
using EarthTool.PAR.Models.Abstracts;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text.Json.Serialization;

namespace EarthTool.PAR.GUI.Services;

/// <summary>
/// Factory for creating property editor ViewModels based on property types.
/// </summary>
public class PropertyEditorFactory : IPropertyEditorFactory
{
  private readonly IUndoRedoService _undoRedoService;
  private readonly ILogger<PropertyEditorFactory> _logger;

  // Properties to exclude from editing
  private static readonly HashSet<string> ExcludedProperties = new()
  {
    nameof(Entity.FieldTypes),
    nameof(Entity.ClassId),
    nameof(Entity.TypeName),
    "ReferenceMarker"
  };

  public PropertyEditorFactory(
    IUndoRedoService undoRedoService,
    ILogger<PropertyEditorFactory> logger)
  {
    _undoRedoService = undoRedoService ?? throw new ArgumentNullException(nameof(undoRedoService));
    _logger = logger ?? throw new ArgumentNullException(nameof(logger));
  }

  /// <inheritdoc/>
  public IEnumerable<PropertyEditorViewModel> CreateEditorsForEntity(Entity entity)
  {
    if (entity == null)
      throw new ArgumentNullException(nameof(entity));

    var properties = entity.GetType()
      .GetProperties(BindingFlags.Public | BindingFlags.Instance)
      .Where(p => p.CanRead && p.CanWrite)
      .Where(p => !ExcludedProperties.Contains(p.Name))
      .Where(p => p.GetCustomAttribute<JsonIgnoreAttribute>() == null);

    var editors = new List<PropertyEditorViewModel>();

    foreach (var property in properties)
    {
      var editor = CreateEditorForProperty(entity, property);
      if (editor != null)
      {
        editors.Add(editor);
      }
    }

    _logger.LogDebug("Created {EditorCount} property editors for entity '{EntityName}'",
      editors.Count, entity.Name);

    return editors;
  }

  /// <inheritdoc/>
  public PropertyEditorViewModel? CreateEditorForProperty(Entity entity, PropertyInfo property)
  {
    if (entity == null || property == null)
      return null;

    var propertyType = property.PropertyType;
    var propertyValue = property.GetValue(entity);

    PropertyEditorViewModel? editor = null;

    // Determine editor type based on property type
    if (propertyType == typeof(int))
    {
      editor = new IntPropertyEditorViewModel(_undoRedoService)
      {
        IntValue = (int)(propertyValue ?? 0)
      };
    }
    else if (propertyType == typeof(string))
    {
      editor = new StringPropertyEditorViewModel(_undoRedoService)
      {
        StringValue = (string)(propertyValue ?? string.Empty),
        IsEntityReference = property.Name.EndsWith("Id")
      };
    }
    else if (propertyType.IsEnum)
    {
      editor = new EnumPropertyEditorViewModel(_undoRedoService)
      {
        EnumType = propertyType,
        Value = propertyValue
      };
    }
    else
    {
      _logger.LogWarning("No editor available for property '{PropertyName}' of type '{PropertyType}'",
        property.Name, propertyType.Name);
      return null;
    }

    // Set common properties
    if (editor != null)
    {
      editor.PropertyName = property.Name;
      editor.DisplayName = FormatPropertyName(property.Name);
      editor.Description = $"{property.Name} ({propertyType.Name})";
    }

    return editor;
  }

  private static string FormatPropertyName(string propertyName)
  {
    if (string.IsNullOrEmpty(propertyName))
      return propertyName;

    // Convert PascalCase to "Pascal Case"
    var result = string.Concat(propertyName.Select((c, i) =>
      i > 0 && char.IsUpper(c) && !char.IsUpper(propertyName[i - 1]) ? " " + c : c.ToString()));

    return result;
  }
}
