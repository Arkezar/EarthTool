using EarthTool.PAR.GUI.Services;
using ReactiveUI;

namespace EarthTool.PAR.GUI.ViewModels;

/// <summary>
/// ViewModel for editing integer properties.
/// </summary>
public class IntPropertyEditorViewModel : PropertyEditorViewModel
{
  private readonly IUndoRedoService? _undoRedoService;
  private int _value;
  private int _minValue = int.MinValue;
  private int _maxValue = int.MaxValue;
  private int _step = 1;
  private string? _unit;

  public IntPropertyEditorViewModel()
  {
    PropertyType = typeof(int);
  }

  public IntPropertyEditorViewModel(IUndoRedoService undoRedoService) : this()
  {
    _undoRedoService = undoRedoService;
  }

  /// <summary>
  /// Gets or sets the integer value.
  /// </summary>
  public int IntValue
  {
    get => _value;
    set
    {
      if (_value == value) return;
      
      var oldValue = _value;
      var newValue = value;
      
      // Record undo action before changing
      _undoRedoService?.RecordAction(
        description: $"Change {DisplayName} from {oldValue} to {newValue}",
        undoCallback: () => { _value = oldValue; this.RaisePropertyChanged(nameof(IntValue)); NotifyValueChanged(); },
        redoCallback: () => { _value = newValue; this.RaisePropertyChanged(nameof(IntValue)); NotifyValueChanged(); }
      );

      this.RaiseAndSetIfChanged(ref _value, newValue);
      NotifyValueChanged();
    }
  }

  /// <inheritdoc/>
  public override object? Value
  {
    get => IntValue;
    set
    {
      if (value is int intValue)
        IntValue = intValue;
    }
  }

  /// <summary>
  /// Gets or sets the minimum value.
  /// </summary>
  public int MinValue
  {
    get => _minValue;
    set => this.RaiseAndSetIfChanged(ref _minValue, value);
  }

  /// <summary>
  /// Gets or sets the maximum value.
  /// </summary>
  public int MaxValue
  {
    get => _maxValue;
    set => this.RaiseAndSetIfChanged(ref _maxValue, value);
  }

  /// <summary>
  /// Gets or sets the step increment.
  /// </summary>
  public int Step
  {
    get => _step;
    set => this.RaiseAndSetIfChanged(ref _step, value);
  }

  /// <summary>
  /// Gets or sets the unit label (e.g., "HP", "sec", "m").
  /// </summary>
  public string? Unit
  {
    get => _unit;
    set => this.RaiseAndSetIfChanged(ref _unit, value);
  }

  /// <inheritdoc/>
  public override bool IsValid => string.IsNullOrEmpty(ErrorMessage);

  /// <inheritdoc/>
  protected override void ValidateValue()
  {
    if (_value < MinValue || _value > MaxValue)
    {
      ErrorMessage = $"Value must be between {MinValue} and {MaxValue}";
    }
    else
    {
      ErrorMessage = null;
    }
  }
}
