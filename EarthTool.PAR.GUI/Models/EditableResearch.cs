using EarthTool.PAR.Models;
using ReactiveUI;
using System;
using System.Collections.Generic;
using System.Reflection;

namespace EarthTool.PAR.GUI.Models;

/// <summary>
/// Wrapper for Research that provides dirty tracking and change management.
/// </summary>
public class EditableResearch : ReactiveObject
{
  private readonly Research _originalResearch;
  private Research _currentResearch;
  private bool _isDirty;
  private readonly Dictionary<string, object?> _originalValues;

  public EditableResearch(Research research)
  {
    _originalResearch = research ?? throw new ArgumentNullException(nameof(research));
    _currentResearch = research;
    _originalValues = new Dictionary<string, object?>();

    // Store original values for all properties
    CaptureOriginalValues();
  }

  /// <summary>
  /// Gets the current research being edited.
  /// </summary>
  public Research Research => _currentResearch;

  /// <summary>
  /// Gets whether the research has unsaved changes.
  /// </summary>
  public bool IsDirty
  {
    get => _isDirty;
    private set => this.RaiseAndSetIfChanged(ref _isDirty, value);
  }

  /// <summary>
  /// Gets the display name of the research.
  /// </summary>
  public string DisplayName => _currentResearch.Name;

  /// <summary>
  /// Gets the type name of the research.
  /// </summary>
  public string TypeName => _currentResearch.GetType().Name;

  /// <summary>
  /// Marks the research as dirty (modified).
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
    if (!IsDirty)
      return;

    // Restore all original values
    foreach (var kvp in _originalValues)
    {
      var property = _currentResearch.GetType().GetProperty(kvp.Key);
      if (property != null && property.CanWrite)
      {
        property.SetValue(_currentResearch, kvp.Value);
      }
    }

    IsDirty = false;
  }

  private void CaptureOriginalValues()
  {
    _originalValues.Clear();
    var properties = _currentResearch.GetType().GetProperties(BindingFlags.Public | BindingFlags.Instance);

    foreach (var property in properties)
    {
      if (property.CanRead)
      {
        var value = property.GetValue(_currentResearch);
        _originalValues[property.Name] = value;
      }
    }
  }
}
