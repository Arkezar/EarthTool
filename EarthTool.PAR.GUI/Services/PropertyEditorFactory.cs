using EarthTool.PAR.GUI.ViewModels;
using EarthTool.PAR.Models.Abstracts;
using Microsoft.Extensions.Logging;
using ReactiveUI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Linq;
using System.Reflection;
using System.Text.Json.Serialization;
using EarthTool.PAR.Models;

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
  public IEnumerable<PropertyEditorViewModel> CreateEditorsForEntity(Entity entity, Action? onPropertyChanged = null)
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
      var editor = CreateEditorForProperty(entity, property, onPropertyChanged);
      if (editor != null)
      {
        editors.Add(editor);
      }
    }

    _logger.LogDebug("Created {EditorCount} property editors for entity '{EntityName}'",
      editors.Count, entity.Name);

    return editors;
  }

  public IEnumerable<PropertyEditorViewModel> CreateEditorsForResearch(Research entity, Action? onPropertyChanged = null)
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
      var editor = CreateEditorForProperty(entity, property, onPropertyChanged);
      if (editor != null)
      {
        editors.Add(editor);
      }
    }

    _logger.LogDebug("Created {EditorCount} property editors for research '{EntityName}'",
      editors.Count, entity.Name);

    return editors;
    
    /*
       
       // Basic properties
       editors.Add(CreateEditor(research, nameof(research.Id), "ID"));
       editors.Add(CreateEditor(research, nameof(research.Name), "Name"));
       editors.Add(CreateEditor(research, nameof(research.Faction), "Faction"));
       editors.Add(CreateEditor(research, nameof(research.Type), "Type"));
       
       // Cost properties
       editors.Add(CreateEditor(research, nameof(research.CampaignCost), "Campaign Cost"));
       editors.Add(CreateEditor(research, nameof(research.SkirmishCost), "Skirmish Cost"));
       
       // Timing properties
       editors.Add(CreateEditor(research, nameof(research.CampaignTime), "Campaign Time"));
       editors.Add(CreateEditor(research, nameof(research.SkirmishTime), "Skirmish Time"));
       
       // Reference properties
       editors.Add(CreateEditor(research, nameof(research.Video), "Video"));
       editors.Add(CreateEditor(research, nameof(research.Mesh), "Mesh"));
       editors.Add(CreateEditor(research, nameof(research.MeshParamsIndex), "Mesh Params Index"));
       
       // Requirements (special handling)
       var requiredResearchValue = string.Join(", ", research.RequiredResearch);
       editors.Add(new PropertyEditorViewModel
       {
         PropertyName = "RequiredResearch",
         DisplayName = "Required Research",
         Value = requiredResearchValue,
         PropertyType = typeof(string),
         IsReadOnly = true
       });
       
       return editors;
     */
  }

  private PropertyEditorViewModel? CreateEditorForProperty(object entity, PropertyInfo property, Action? onPropertyChanged = null)
  {
    if (entity == null || property == null)
      return null;

    var propertyType = property.PropertyType;
    var propertyValue = property.GetValue(entity);

    PropertyEditorViewModel? editor = null;

    // Determine editor type based on property type
    if (propertyType == typeof(int))
    {
      var intEditor = new IntPropertyEditorViewModel(_undoRedoService)
      {
        IntValue = (int)(propertyValue ?? 0)
      };
      
      // Subscribe to value changes to update the entity
      intEditor.WhenAnyValue(x => x.IntValue)
        .Skip(1) // Skip initial value
        .Subscribe(newValue =>
        {
          property.SetValue(entity, newValue);
          onPropertyChanged?.Invoke();
        });
      
      editor = intEditor;
    }
    else if (propertyType == typeof(string))
    {
      var stringEditor = new StringPropertyEditorViewModel(_undoRedoService)
      {
        StringValue = (string)(propertyValue ?? string.Empty),
        IsEntityReferenceValue = property.Name.EndsWith("Id")
      };
      
      // Subscribe to value changes to update the entity
      stringEditor.WhenAnyValue(x => x.StringValue)
        .Skip(1) // Skip initial value
        .Subscribe(newValue =>
        {
          property.SetValue(entity, newValue);
          onPropertyChanged?.Invoke();
        });
      
      editor = stringEditor;
    }
    else if (propertyType.IsEnum)
    {
      var enumEditor = new EnumPropertyEditorViewModel(_undoRedoService)
      {
        EnumType = propertyType,
        Value = propertyValue
      };
      
      // Subscribe to value changes to update the entity
      enumEditor.WhenAnyValue(x => x.Value)
        .Skip(1) // Skip initial value
        .Subscribe(newValue =>
        {
          if (newValue != null)
          {
            property.SetValue(entity, newValue);
            onPropertyChanged?.Invoke();
          }
        });
      
      editor = enumEditor;
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
