using EarthTool.PAR.GUI.Models;
using ReactiveUI;
using System;
using System.Reactive;
using System.Reactive.Linq;

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
  private ValidationSeverity _validationSeverity = ValidationSeverity.Error;

  /// <summary>
  /// Gets the command to copy the property value to clipboard.
  /// </summary>
  public ReactiveCommand<Unit, Unit> CopyValueCommand { get; }

  /// <summary>
  /// Gets the command to navigate to the referenced entity.
  /// </summary>
  public ReactiveCommand<Unit, Unit> NavigateToReferenceCommand { get; protected set; }

  /// <summary>
  /// Gets whether this property is an entity reference (e.g., ends with "Id").
  /// </summary>
  public virtual bool IsEntityReference => false;

  public PropertyEditorViewModel()
  {
    CopyValueCommand = ReactiveCommand.Create(() =>
    {
      // Clipboard will be accessed from UI layer via Interaction
      // For now, just expose the command
    });

    // Default disabled command - derived classes can override
    NavigateToReferenceCommand = ReactiveCommand.Create(
      () => { }, 
      Observable.Return(false));
  }

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
  /// Gets or sets the validation severity (Error, Warning, Info).
  /// </summary>
  public ValidationSeverity ValidationSeverity
  {
    get => _validationSeverity;
    set => this.RaiseAndSetIfChanged(ref _validationSeverity, value);
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
