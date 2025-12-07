using EarthTool.PAR.GUI.Services;
using ReactiveUI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Linq;

namespace EarthTool.PAR.GUI.ViewModels;

/// <summary>
/// ViewModel for editing collections of integers (e.g., RequiredResearch).
/// </summary>
public class IntCollectionPropertyEditorViewModel : PropertyEditorViewModel
{
  private readonly IUndoRedoService? _undoRedoService;
  private string _stringValue = string.Empty;
  private IEnumerable<int>? _collectionValue;

  public IntCollectionPropertyEditorViewModel()
  {
    PropertyType = typeof(IEnumerable<int>);
  }

  public IntCollectionPropertyEditorViewModel(IUndoRedoService undoRedoService) : this()
  {
    _undoRedoService = undoRedoService;
  }

  /// <summary>
  /// Gets or sets the string representation of the collection (comma-separated).
  /// </summary>
  public string StringValue
  {
    get => _stringValue;
    set
    {
      if (_stringValue == value) return;
      
      var oldStringValue = _stringValue;
      var newStringValue = value ?? string.Empty;
      
      // Try to parse the string to collection
      var parseSuccess = TryParseStringToCollection(newStringValue, out var newCollection);
      if (!parseSuccess)
      {
        // If parsing fails, just update the string value and set error
        this.RaiseAndSetIfChanged(ref _stringValue, newStringValue);
        ErrorMessage = "Invalid format. Use comma-separated integers (e.g., 1, 2, 3)";
        this.RaisePropertyChanged(nameof(IsValid));
        return;
      }

      var oldCollection = _collectionValue;

      // Record undo action
      _undoRedoService?.RecordAction(
        description: $"Change {DisplayName} from '{oldStringValue}' to '{newStringValue}'",
        undoCallback: () => 
        { 
          _stringValue = oldStringValue; 
          _collectionValue = oldCollection;
          this.RaisePropertyChanged(nameof(StringValue)); 
          this.RaisePropertyChanged(nameof(Value));
          NotifyValueChanged(); 
        },
        redoCallback: () => 
        { 
          _stringValue = newStringValue; 
          _collectionValue = newCollection;
          this.RaisePropertyChanged(nameof(StringValue)); 
          this.RaisePropertyChanged(nameof(Value));
          NotifyValueChanged(); 
        }
      );

      this.RaiseAndSetIfChanged(ref _stringValue, newStringValue);
      _collectionValue = newCollection;
      this.RaisePropertyChanged(nameof(Value));
      NotifyValueChanged();
    }
  }

  /// <inheritdoc/>
  public override object? Value
  {
    get => _collectionValue;
    set
    {
      if (value is IEnumerable<int> collection)
      {
        _collectionValue = collection;
        _stringValue = string.Join(", ", collection);
        this.RaisePropertyChanged(nameof(StringValue));
      }
      else if (value == null)
      {
        _collectionValue = Enumerable.Empty<int>();
        _stringValue = string.Empty;
        this.RaisePropertyChanged(nameof(StringValue));
      }
      
      this.RaisePropertyChanged();
      NotifyValueChanged();
    }
  }

  /// <inheritdoc/>
  public override bool IsValid => string.IsNullOrEmpty(ErrorMessage);

  /// <inheritdoc/>
  protected override void ValidateValue()
  {
    if (IsRequired && string.IsNullOrWhiteSpace(_stringValue))
    {
      ErrorMessage = $"{DisplayName} is required";
    }
    else if (!string.IsNullOrWhiteSpace(_stringValue) && !TryParseStringToCollection(_stringValue, out _))
    {
      ErrorMessage = "Invalid format. Use comma-separated integers (e.g., 1, 2, 3)";
    }
    else
    {
      ErrorMessage = null;
    }
  }

  private static bool TryParseStringToCollection(string input, out IEnumerable<int> result)
  {
    result = Enumerable.Empty<int>();

    if (string.IsNullOrWhiteSpace(input))
    {
      // Empty input is valid - empty collection
      return true;
    }

    var parts = input.Split(new[] { ',', ';' }, StringSplitOptions.RemoveEmptyEntries);
    var parsedInts = new List<int>();

    foreach (var part in parts)
    {
      if (int.TryParse(part.Trim(), out var value))
      {
        parsedInts.Add(value);
      }
      else
      {
        // Failed to parse a part
        return false;
      }
    }

    result = parsedInts;
    return true;
  }
}
