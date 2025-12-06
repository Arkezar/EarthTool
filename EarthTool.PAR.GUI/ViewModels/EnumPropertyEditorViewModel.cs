using EarthTool.PAR.GUI.Services;
using ReactiveUI;
using System;
using System.Collections.ObjectModel;
using System.Linq;

namespace EarthTool.PAR.GUI.ViewModels;

/// <summary>
/// ViewModel for editing enum properties.
/// </summary>
public class EnumPropertyEditorViewModel : PropertyEditorViewModel
{
  private readonly IUndoRedoService? _undoRedoService;
  private object? _value;
  private Type? _enumType;

  public EnumPropertyEditorViewModel()
  {
    AvailableValues = new ObservableCollection<EnumValueViewModel>();
  }

  public EnumPropertyEditorViewModel(IUndoRedoService undoRedoService) : this()
  {
    _undoRedoService = undoRedoService;
  }

  /// <summary>
  /// Gets or sets the enum type.
  /// </summary>
  public Type? EnumType
  {
    get => _enumType;
    set
    {
      if (_enumType == value) return;
      
      _enumType = value;
      PropertyType = value ?? typeof(object);
      LoadEnumValues();
      this.RaisePropertyChanged();
    }
  }

  /// <summary>
  /// Gets the collection of available enum values.
  /// </summary>
  public ObservableCollection<EnumValueViewModel> AvailableValues { get; }

  /// <summary>
  /// Gets or sets the currently selected enum value.
  /// </summary>
  public EnumValueViewModel? SelectedValue
  {
    get => AvailableValues.FirstOrDefault(v => Equals(v.Value, _value));
    set
    {
      if (value == null) return;
      
      var oldValue = _value;
      var newValue = value.Value;
      
      if (Equals(oldValue, newValue)) return;
      
      // Record undo action
      _undoRedoService?.RecordAction(
        description: $"Change {DisplayName} from {oldValue} to {newValue}",
        undoCallback: () => { _value = oldValue; this.RaisePropertyChanged(nameof(SelectedValue)); this.RaisePropertyChanged(nameof(Value)); NotifyValueChanged(); },
        redoCallback: () => { _value = newValue; this.RaisePropertyChanged(nameof(SelectedValue)); this.RaisePropertyChanged(nameof(Value)); NotifyValueChanged(); }
      );

      _value = newValue;
      this.RaisePropertyChanged(nameof(SelectedValue));
      this.RaisePropertyChanged(nameof(Value));
      NotifyValueChanged();
    }
  }

  /// <inheritdoc/>
  public override object? Value
  {
    get => _value;
    set
    {
      if (Equals(_value, value)) return;
      
      _value = value;
      this.RaisePropertyChanged();
      this.RaisePropertyChanged(nameof(SelectedValue));
      NotifyValueChanged();
    }
  }

  /// <inheritdoc/>
  public override bool IsValid => string.IsNullOrEmpty(ErrorMessage);

  /// <inheritdoc/>
  protected override void ValidateValue()
  {
    if (IsRequired && _value == null)
    {
      ErrorMessage = $"{DisplayName} is required";
    }
    else if (_enumType != null && _value != null && !Enum.IsDefined(_enumType, _value))
    {
      ErrorMessage = $"Invalid value for {DisplayName}";
    }
    else
    {
      ErrorMessage = null;
    }
  }

  private void LoadEnumValues()
  {
    AvailableValues.Clear();

    if (_enumType == null || !_enumType.IsEnum)
      return;

    foreach (var value in Enum.GetValues(_enumType))
    {
      var name = Enum.GetName(_enumType, value);
      if (name == null) continue;

      var enumValue = new EnumValueViewModel
      {
        Value = value,
        DisplayName = FormatEnumName(name),
        NumericValue = Convert.ToUInt32(value),
        Description = name // TODO: Get from Description attribute if available
      };

      AvailableValues.Add(enumValue);
    }
  }

  private static string FormatEnumName(string name)
  {
    // Convert PascalCase to "Pascal Case"
    if (string.IsNullOrEmpty(name))
      return name;

    var result = string.Concat(name.Select((c, i) =>
      i > 0 && char.IsUpper(c) && !char.IsUpper(name[i - 1]) ? " " + c : c.ToString()));

    return result;
  }
}

/// <summary>
/// ViewModel for a single enum value option.
/// </summary>
public class EnumValueViewModel
{
  /// <summary>
  /// Gets or sets the actual enum value.
  /// </summary>
  public object Value { get; set; } = 0;

  /// <summary>
  /// Gets or sets the display name.
  /// </summary>
  public string DisplayName { get; set; } = string.Empty;

  /// <summary>
  /// Gets or sets the numeric value.
  /// </summary>
  public uint NumericValue { get; set; }

  /// <summary>
  /// Gets or sets the description.
  /// </summary>
  public string Description { get; set; } = string.Empty;
}
