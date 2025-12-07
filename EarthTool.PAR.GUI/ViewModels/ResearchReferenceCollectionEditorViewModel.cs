using EarthTool.PAR.GUI.Services;
using EarthTool.PAR.Models;
using ReactiveUI;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Reactive;
using System.Reactive.Linq;

namespace EarthTool.PAR.GUI.ViewModels;

/// <summary>
/// ViewModel for editing collections of research IDs with name mapping.
/// </summary>
public class ResearchReferenceCollectionEditorViewModel : PropertyEditorViewModel
{
  private readonly IUndoRedoService? _undoRedoService;
  private IEnumerable<int>? _collectionValue;
  private ParFile? _parFile;
  private bool _isUpdating;
  private Action<string>? _navigateToResearchAction;
  private ResearchReferenceViewModel? _selectedAvailableResearch;

  public ResearchReferenceCollectionEditorViewModel()
  {
    PropertyType = typeof(IEnumerable<int>);
    AvailableResearch = new ObservableCollection<ResearchReferenceViewModel>();
    SelectedResearch = new ObservableCollection<ResearchReferenceViewModel>();
    
    // Initialize commands
    AddResearchCommand = ReactiveCommand.Create<ResearchReferenceViewModel>(AddResearch, 
      this.WhenAnyValue(x => x.SelectedAvailableResearch).Select(r => r != null));
    RemoveResearchCommand = ReactiveCommand.Create<ResearchReferenceViewModel>(RemoveResearch);
  }

  public ResearchReferenceCollectionEditorViewModel(IUndoRedoService undoRedoService) : this()
  {
    _undoRedoService = undoRedoService;
  }

  /// <summary>
  /// Sets the navigation action for navigating to research by name.
  /// </summary>
  public Action<string>? NavigateToResearchAction
  {
    get => _navigateToResearchAction;
    set
    {
      _navigateToResearchAction = value;
      // Update navigate commands for existing items
      foreach (var research in AvailableResearch)
      {
        UpdateNavigateCommand(research);
      }
    }
  }

  /// <summary>
  /// Gets the collection of available research items.
  /// </summary>
  public ObservableCollection<ResearchReferenceViewModel> AvailableResearch { get; }

  /// <summary>
  /// Gets the collection of currently selected research items.
  /// </summary>
  public ObservableCollection<ResearchReferenceViewModel> SelectedResearch { get; }

  /// <summary>
  /// Gets or sets the selected research from the available list (for adding).
  /// </summary>
  public ResearchReferenceViewModel? SelectedAvailableResearch
  {
    get => _selectedAvailableResearch;
    set => this.RaiseAndSetIfChanged(ref _selectedAvailableResearch, value);
  }

  /// <summary>
  /// Gets the command to add a research to the selected list.
  /// </summary>
  public ReactiveCommand<ResearchReferenceViewModel, Unit> AddResearchCommand { get; }

  /// <summary>
  /// Gets the command to remove a research from the selected list.
  /// </summary>
  public ReactiveCommand<ResearchReferenceViewModel, Unit> RemoveResearchCommand { get; }

  /// <summary>
  /// Sets the ParFile context to enable research name lookup.
  /// </summary>
  public ParFile? ParFileContext
  {
    get => _parFile;
    set
    {
      if (_parFile == value)
        return;
        
      _parFile = value;
      LoadAvailableResearch();
      
      // Update selection after loading available research
      if (_collectionValue != null)
      {
        UpdateSelectedResearch();
      }
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
        UpdateSelectedResearch();
      }
      else if (value == null)
      {
        _collectionValue = Enumerable.Empty<int>();
        UpdateSelectedResearch();
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
    if (IsRequired && (_collectionValue == null || !_collectionValue.Any()))
    {
      ErrorMessage = $"{DisplayName} is required";
    }
    else
    {
      ErrorMessage = null;
    }
  }

  private void LoadAvailableResearch()
  {
    AvailableResearch.Clear();

    if (_parFile == null)
      return;

    foreach (var research in _parFile.Research.OrderBy(r => r.Name))
    {
      var vm = new ResearchReferenceViewModel
      {
        Id = research.Id,
        Name = research.Name,
        Type = research.Type.ToString(),
        Faction = research.Faction.ToString()
      };

      // Set up navigate command
      UpdateNavigateCommand(vm);

      AvailableResearch.Add(vm);
    }
  }

  private void UpdateNavigateCommand(ResearchReferenceViewModel research)
  {
    if (_navigateToResearchAction != null)
    {
      research.NavigateCommand = ReactiveCommand.Create(() =>
      {
        _navigateToResearchAction?.Invoke(research.Name);
      });
    }
  }

  private void AddResearch(ResearchReferenceViewModel? research)
  {
    if (research == null || _isUpdating)
      return;

    // Check if already selected
    if (SelectedResearch.Any(r => r.Id == research.Id))
      return;

    var oldValue = _collectionValue;
    var newList = (oldValue?.ToList() ?? new List<int>());
    newList.Add(research.Id);
    var newValue = newList.AsEnumerable();

    // Record undo action
    _undoRedoService?.RecordAction(
      description: $"Add {research.Name} to {DisplayName}",
      undoCallback: () => { _collectionValue = oldValue; UpdateSelectedResearch(); this.RaisePropertyChanged(nameof(Value)); NotifyValueChanged(); },
      redoCallback: () => { _collectionValue = newValue; UpdateSelectedResearch(); this.RaisePropertyChanged(nameof(Value)); NotifyValueChanged(); }
    );

    _collectionValue = newValue;
    UpdateSelectedResearch();
    this.RaisePropertyChanged(nameof(Value));
    NotifyValueChanged();

    // Clear selection
    SelectedAvailableResearch = null;
  }

  private void RemoveResearch(ResearchReferenceViewModel? research)
  {
    if (research == null || _isUpdating)
      return;

    var oldValue = _collectionValue;
    var newList = (oldValue?.ToList() ?? new List<int>());
    newList.Remove(research.Id);
    var newValue = newList.AsEnumerable();

    // Record undo action
    _undoRedoService?.RecordAction(
      description: $"Remove {research.Name} from {DisplayName}",
      undoCallback: () => { _collectionValue = oldValue; UpdateSelectedResearch(); this.RaisePropertyChanged(nameof(Value)); NotifyValueChanged(); },
      redoCallback: () => { _collectionValue = newValue; UpdateSelectedResearch(); this.RaisePropertyChanged(nameof(Value)); NotifyValueChanged(); }
    );

    _collectionValue = newValue;
    UpdateSelectedResearch();
    this.RaisePropertyChanged(nameof(Value));
    NotifyValueChanged();
  }

  private void UpdateSelectedResearch()
  {
    _isUpdating = true;
    try
    {
      SelectedResearch.Clear();

      if (_parFile == null || _collectionValue == null)
        return;

      var selectedIds = new HashSet<int>(_collectionValue);

      // Find research items by ID and add to selected list
      foreach (var id in _collectionValue)
      {
        var research = AvailableResearch.FirstOrDefault(r => r.Id == id);
        if (research != null)
        {
          // Set up remove command for this selected item
          research.RemoveCommand = ReactiveCommand.Create(() => RemoveResearch(research));
          SelectedResearch.Add(research);
        }
      }
    }
    finally
    {
      _isUpdating = false;
    }
  }
}

/// <summary>
/// ViewModel for a research reference item.
/// </summary>
public class ResearchReferenceViewModel : ReactiveObject
{
  private bool _isSelected;

  public ResearchReferenceViewModel()
  {
    NavigateCommand = ReactiveCommand.Create(() => { });
    RemoveCommand = ReactiveCommand.Create(() => { });
  }

  /// <summary>
  /// Gets or sets the research ID.
  /// </summary>
  public int Id { get; set; }

  /// <summary>
  /// Gets or sets the research name.
  /// </summary>
  public string Name { get; set; } = string.Empty;

  /// <summary>
  /// Gets or sets the research type.
  /// </summary>
  public string Type { get; set; } = string.Empty;

  /// <summary>
  /// Gets or sets the faction.
  /// </summary>
  public string Faction { get; set; } = string.Empty;

  /// <summary>
  /// Gets or sets whether this research is selected.
  /// </summary>
  public bool IsSelected
  {
    get => _isSelected;
    set => this.RaiseAndSetIfChanged(ref _isSelected, value);
  }

  /// <summary>
  /// Gets the display text for this research.
  /// </summary>
  public string DisplayText => $"{Name} (ID: {Id})";

  /// <summary>
  /// Gets the tooltip text.
  /// </summary>
  public string ToolTipText => $"{Name}\nID: {Id}\nType: {Type}\nFaction: {Faction}";

  /// <summary>
  /// Gets the command to navigate to this research.
  /// </summary>
  public ReactiveCommand<Unit, Unit> NavigateCommand { get; set; }

  /// <summary>
  /// Gets the command to remove this research from the selected list.
  /// </summary>
  public ReactiveCommand<Unit, Unit> RemoveCommand { get; set; }

  /// <summary>
  /// Returns the display text for AutoCompleteBox.
  /// </summary>
  public override string ToString() => DisplayText;
}
