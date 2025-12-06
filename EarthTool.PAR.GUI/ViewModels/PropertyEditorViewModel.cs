using ReactiveUI;
using System;

namespace EarthTool.PAR.GUI.ViewModels;

/// <summary>
/// Base ViewModel for property editors.
/// </summary>
public abstract class PropertyEditorViewModel : ViewModelBase
{
  private string _propertyName = string.Empty;
  private string _displayName = string.Empty;
  private string _description = string.Empty;
  private bool _isReadOnly;
  private bool _isRequired;
  private string? _errorMessage;

  /// <summary>
  /// Gets or sets the property name (as it appears in the Entity class).
  /// </summary>
  public string PropertyName
  {
    get => _propertyName;
    set => this.RaiseAndSetIfChanged(ref _propertyName, value);
  }

  /// <summary>
  /// Gets or sets the display name (user-friendly name).
  /// </summary>
  public string DisplayName
  {
    get => _displayName;
    set => this.RaiseAndSetIfChanged(ref _displayName, value);
  }

  /// <summary>
  /// Gets or sets the description/tooltip for the property.
  /// </summary>
  public string Description
  {
    get => _description;
    set => this.RaiseAndSetIfChanged(ref _description, value);
  }

  /// <summary>
  /// Gets or sets the type of the property.
  /// </summary>
  public Type PropertyType { get; set; } = typeof(object);

  /// <summary>
  /// Gets or sets whether the property is read-only.
  /// </summary>
  public bool IsReadOnly
  {
    get => _isReadOnly;
    set => this.RaiseAndSetIfChanged(ref _isReadOnly, value);
  }

  /// <summary>
  /// Gets or sets whether the property is required.
  /// </summary>
  public bool IsRequired
  {
    get => _isRequired;
    set => this.RaiseAndSetIfChanged(ref _isRequired, value);
  }

  /// <summary>
  /// Gets or sets the error message for validation.
  /// </summary>
  public string? ErrorMessage
  {
    get => _errorMessage;
    protected set => this.RaiseAndSetIfChanged(ref _errorMessage, value);
  }

  /// <summary>
  /// Gets whether the current value is valid.
  /// </summary>
  public abstract bool IsValid { get; }

  /// <summary>
  /// Gets or sets the property value.
  /// </summary>
  public abstract object? Value { get; set; }

  /// <summary>
  /// Validates the current value.
  /// </summary>
  protected abstract void ValidateValue();

  /// <summary>
  /// Notifies that the value has changed.
  /// </summary>
  protected void NotifyValueChanged()
  {
    this.RaisePropertyChanged(nameof(Value));
    this.RaisePropertyChanged(nameof(IsValid));
    ValidateValue();
  }
}
