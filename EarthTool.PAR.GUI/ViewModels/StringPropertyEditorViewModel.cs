using EarthTool.PAR.GUI.Services;
using ReactiveUI;
using System.Collections.ObjectModel;
using System.Reactive.Linq;

namespace EarthTool.PAR.GUI.ViewModels;

/// <summary>
/// ViewModel for editing string properties.
/// </summary>
public class StringPropertyEditorViewModel : PropertyEditorViewModel
{
  private readonly IUndoRedoService? _undoRedoService;
  private string _value = string.Empty;
  private int _maxLength = 256;
  private bool _isMultiline;
  private string? _placeholderText;
  private bool _isEntityReference;

  public StringPropertyEditorViewModel()
  {
    PropertyType = typeof(string);
    SuggestedValues = new ObservableCollection<string>();
  }

  public StringPropertyEditorViewModel(IUndoRedoService undoRedoService) : this()
  {
    _undoRedoService = undoRedoService;
  }

  /// <summary>
  /// Gets or sets the string value.
  /// </summary>
  public string StringValue
  {
    get => _value;
    set
    {
      if (_value == value) return;
      
      var oldValue = _value;
      var newValue = value ?? string.Empty;
      
      // Record undo action
      _undoRedoService?.RecordAction(
        description: $"Change {DisplayName} from '{oldValue}' to '{newValue}'",
        undoCallback: () => { _value = oldValue; this.RaisePropertyChanged(nameof(StringValue)); NotifyValueChanged(); },
        redoCallback: () => { _value = newValue; this.RaisePropertyChanged(nameof(StringValue)); NotifyValueChanged(); }
      );

      this.RaiseAndSetIfChanged(ref _value, newValue);
      NotifyValueChanged();
    }
  }

  /// <inheritdoc/>
  public override object? Value
  {
    get => StringValue;
    set
    {
      if (value is string strValue)
        StringValue = strValue;
      else if (value == null)
        StringValue = string.Empty;
    }
  }

  /// <summary>
  /// Gets or sets the maximum length.
  /// </summary>
  public int MaxLength
  {
    get => _maxLength;
    set => this.RaiseAndSetIfChanged(ref _maxLength, value);
  }

  /// <summary>
  /// Gets or sets whether this is a multiline editor.
  /// </summary>
  public bool IsMultiline
  {
    get => _isMultiline;
    set => this.RaiseAndSetIfChanged(ref _isMultiline, value);
  }

  /// <summary>
  /// Gets or sets the placeholder text.
  /// </summary>
  public string? PlaceholderText
  {
    get => _placeholderText;
    set => this.RaiseAndSetIfChanged(ref _placeholderText, value);
  }

  /// <summary>
  /// Gets or sets whether this is an entity reference.
  /// </summary>
  public override bool IsEntityReference => _isEntityReference;
  
  /// <summary>
  /// Sets whether this is an entity reference.
  /// </summary>
  public bool IsEntityReferenceValue
  {
    get => _isEntityReference;
    set
    {
      if (this.RaiseAndSetIfChanged(ref _isEntityReference, value) && _isEntityReference)
      {
        // Reinitialize navigate command when reference status changes to true
        var canNavigate = this.WhenAnyValue(
          x => x.StringValue,
          val => !string.IsNullOrEmpty(val))
          .StartWith(!string.IsNullOrEmpty(_value)); // Emit initial value immediately

        NavigateToReferenceCommand = ReactiveCommand.Create(() =>
        {
          // Navigation will be handled in UI layer
        }, canNavigate);
      }
    }
  }

  /// <summary>
  /// Gets the collection of suggested values for auto-complete.
  /// </summary>
  public ObservableCollection<string> SuggestedValues { get; }

  /// <inheritdoc/>
  public override bool IsValid => string.IsNullOrEmpty(ErrorMessage);

  /// <inheritdoc/>
  protected override void ValidateValue()
  {
    if (IsRequired && string.IsNullOrWhiteSpace(_value))
    {
      ErrorMessage = $"{DisplayName} is required";
    }
    else if (_value.Length > MaxLength)
    {
      ErrorMessage = $"{DisplayName} cannot exceed {MaxLength} characters";
    }
    else
    {
      ErrorMessage = null;
    }
  }
}
