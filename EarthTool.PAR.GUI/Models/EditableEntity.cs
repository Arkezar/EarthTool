using EarthTool.PAR.Enums;
using EarthTool.PAR.Models.Abstracts;
using ReactiveUI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace EarthTool.PAR.GUI.Models;

/// <summary>
/// Wrapper for Entity that provides dirty tracking and change management.
/// </summary>
public class EditableEntity : ReactiveObject
{
  private readonly Entity _originalEntity;
  private Entity _currentEntity;
  private bool _isDirty;
  private readonly Dictionary<string, object?> _originalValues;

  public EditableEntity(Entity entity)
  {
    _originalEntity = entity ?? throw new ArgumentNullException(nameof(entity));
    _currentEntity = entity;
    _originalValues = new Dictionary<string, object?>();

    // Store original values for all properties
    CaptureOriginalValues();
  }

  /// <summary>
  /// Gets the current entity being edited.
  /// </summary>
  public Entity Entity => _currentEntity;

  /// <summary>
  /// Gets whether the entity has unsaved changes.
  /// </summary>
  public bool IsDirty
  {
    get => _isDirty;
    private set => this.RaiseAndSetIfChanged(ref _isDirty, value);
  }

  /// <summary>
  /// Gets the display name of the entity.
  /// </summary>
  public string DisplayName => _currentEntity.Name;

  /// <summary>
  /// Gets the type name of the entity.
  /// </summary>
  public string TypeName => _currentEntity.GetType().Name;

  /// <summary>
  /// Gets the class type of the entity.
  /// </summary>
  public EntityClassType ClassType => _currentEntity.ClassId;

  /// <summary>
  /// Marks the entity as dirty (modified).
  /// </summary>
  public void MarkDirty()
  {
    IsDirty = true;
  }

  /// <summary>
  /// Accepts all changes and clears the dirty flag.
  /// </summary>
  public void AcceptChanges()
  {
    CaptureOriginalValues();
    IsDirty = false;
  }

  /// <summary>
  /// Reverts all changes to the original values.
  /// </summary>
  public void RevertChanges()
  {
    foreach (var kvp in _originalValues)
    {
      var property = _currentEntity.GetType().GetProperty(kvp.Key);
      if (property != null && property.CanWrite)
      {
        property.SetValue(_currentEntity, kvp.Value);
      }
    }

    IsDirty = false;
  }

  /// <summary>
  /// Gets a dictionary of properties that have changed from their original values.
  /// </summary>
  public Dictionary<string, (object? OldValue, object? NewValue)> GetChangedProperties()
  {
    var changes = new Dictionary<string, (object?, object?)>();

    foreach (var kvp in _originalValues)
    {
      var property = _currentEntity.GetType().GetProperty(kvp.Key);
      if (property == null) continue;

      var currentValue = property.GetValue(_currentEntity);
      var originalValue = kvp.Value;

      if (!Equals(currentValue, originalValue))
      {
        changes[kvp.Key] = (originalValue, currentValue);
      }
    }

    return changes;
  }

  /// <summary>
  /// Gets the original value of a property.
  /// </summary>
  public object? GetOriginalValue(string propertyName)
  {
    return _originalValues.TryGetValue(propertyName, out var value) ? value : null;
  }

  /// <summary>
  /// Captures the current values of all properties as the "original" state.
  /// </summary>
  private void CaptureOriginalValues()
  {
    _originalValues.Clear();

    var properties = _currentEntity.GetType().GetProperties(BindingFlags.Public | BindingFlags.Instance);
    foreach (var property in properties)
    {
      if (property.CanRead)
      {
        var value = property.GetValue(_currentEntity);

        // For collections, store a copy
        if (value is IEnumerable<int> intList)
        {
          _originalValues[property.Name] = intList.ToList();
        }
        else
        {
          _originalValues[property.Name] = value;
        }
      }
    }
  }
}
