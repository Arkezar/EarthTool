using EarthTool.PAR.GUI.Services;
using ReactiveUI;
using System;
using System.Collections.ObjectModel;
using System.Linq;

namespace EarthTool.PAR.GUI.ViewModels;

/// <summary>
/// ViewModel for editing flags enum properties.
/// </summary>
public class FlagsPropertyEditorViewModel : PropertyEditorViewModel
{
  private readonly IUndoRedoService? _undoRedoService;
  private object? _value;
  private Type? _enumType;

  public FlagsPropertyEditorViewModel()
  {
    AvailableFlags = new ObservableCollection<FlagValueViewModel>();
  }

  public FlagsPropertyEditorViewModel(IUndoRedoService undoRedoService) : this()
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
      LoadFlagValues();
      this.RaisePropertyChanged();
    }
  }

  /// <summary>
  /// Gets the collection of available flag values.
  /// </summary>
  public ObservableCollection<FlagValueViewModel> AvailableFlags { get; }

  /// <inheritdoc/>
  public override object? Value
  {
    get => _value;
    set
    {
      if (Equals(_value, value)) return;
      
      _value = value;
      UpdateFlagSelections();
      this.RaisePropertyChanged();
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
    else if (_enumType != null && _value != null && !IsValidFlagsValue(_enumType, _value))
    {
      ErrorMessage = $"Invalid value for {DisplayName}";
    }
    else
    {
      ErrorMessage = null;
    }
  }

  private static bool IsValidFlagsValue(Type enumType, object value)
  {
    if (!enumType.IsEnum)
      return false;

    // Check if enum has [Flags] attribute
    var isFlagsEnum = enumType.GetCustomAttributes(typeof(FlagsAttribute), false).Length > 0;
    if (!isFlagsEnum)
      return false;

    // For flags enums, check if the value is a valid combination of defined flags
    var underlyingValue = Convert.ToInt64(value);
    
    // Get all defined values
    var allFlags = Enum.GetValues(enumType)
      .Cast<object>()
      .Select(Convert.ToInt64)
      .Aggregate(0L, (current, flag) => current | flag);

    // Check if value contains only valid flags
    return (underlyingValue & ~allFlags) == 0;
  }

  private void LoadFlagValues()
  {
    AvailableFlags.Clear();

    if (_enumType == null || !_enumType.IsEnum)
      return;

    // Check if this is a flags enum
    var isFlagsEnum = _enumType.GetCustomAttributes(typeof(FlagsAttribute), false).Length > 0;
    if (!isFlagsEnum)
      return;

    foreach (var value in Enum.GetValues(_enumType))
    {
      var name = Enum.GetName(_enumType, value);
      if (name == null) continue;

      var numericValue = Convert.ToInt64(value);
      
      // Skip "None" or zero values, or composite flags
      if (numericValue == 0)
        continue;
      
      // Only include power-of-two values (individual flags)
      if (!IsPowerOfTwo(numericValue))
        continue;

      var flagVm = new FlagValueViewModel
      {
        Value = value,
        DisplayName = FormatEnumName(name),
        NumericValue = numericValue,
        Description = name
      };

      // Subscribe to IsSelected changes
      flagVm.WhenAnyValue(f => f.IsSelected)
        .Subscribe(_ => OnFlagSelectionChanged());

      AvailableFlags.Add(flagVm);
    }

    UpdateFlagSelections();
  }

  private static bool IsPowerOfTwo(long n)
  {
    return n > 0 && (n & (n - 1)) == 0;
  }

  private void UpdateFlagSelections()
  {
    if (_value == null || _enumType == null)
      return;

    var currentValue = Convert.ToInt64(_value);

    foreach (var flag in AvailableFlags)
    {
      // Temporarily unsubscribe to avoid triggering change events
      flag.IsSelected = (currentValue & flag.NumericValue) == flag.NumericValue;
    }
  }

  private void OnFlagSelectionChanged()
  {
    if (_enumType == null)
      return;

    var oldValue = _value;

    // Calculate new value from selected flags
    long newNumericValue = 0;
    foreach (var flag in AvailableFlags.Where(f => f.IsSelected))
    {
      newNumericValue |= flag.NumericValue;
    }

    var newValue = Enum.ToObject(_enumType, newNumericValue);

    if (Equals(oldValue, newValue))
      return;

    // Record undo action
    _undoRedoService?.RecordAction(
      description: $"Change {DisplayName} from {oldValue} to {newValue}",
      undoCallback: () => { _value = oldValue; UpdateFlagSelections(); this.RaisePropertyChanged(nameof(Value)); NotifyValueChanged(); },
      redoCallback: () => { _value = newValue; UpdateFlagSelections(); this.RaisePropertyChanged(nameof(Value)); NotifyValueChanged(); }
    );

    _value = newValue;
    this.RaisePropertyChanged(nameof(Value));
    NotifyValueChanged();
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
/// ViewModel for a single flag value option.
/// </summary>
public class FlagValueViewModel : ReactiveObject
{
  private bool _isSelected;

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
  public long NumericValue { get; set; }

  /// <summary>
  /// Gets or sets the description.
  /// </summary>
  public string Description { get; set; } = string.Empty;

  /// <summary>
  /// Gets or sets whether this flag is selected.
  /// </summary>
  public bool IsSelected
  {
    get => _isSelected;
    set => this.RaiseAndSetIfChanged(ref _isSelected, value);
  }
}
