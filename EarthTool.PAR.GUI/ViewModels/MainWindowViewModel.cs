using Avalonia.Controls;
using Avalonia.Controls.Models.TreeDataGrid;
using Avalonia.Controls.Selection;
using DynamicData;
using EarthTool.Common.Interfaces;
using EarthTool.PAR.GUI.Extensions;
using EarthTool.PAR.GUI.Models;
using EarthTool.PAR.GUI.Services;
using EarthTool.PAR.Models;
using ReactiveUI;
using System.Collections.ObjectModel;
using System.Linq;
using System.Reactive;
using System.Reactive.Linq;
using System.Windows.Input;

namespace EarthTool.PAR.GUI.ViewModels;

public class MainWindowViewModel : ViewModelBase
{
  private readonly IReader<ParFile>     _reader;
  private readonly IWriter<ParFile>     _writer;
  private readonly ParameterTreeBuilder _treeBuilder;
  private          string?              _filePath;
  private          object?              _selectedItem;
  private          int                  _totalEntries;
  private          ParFile?             _currentParFile;
  private          bool                 _hasUnsavedChanges;
  private          string               _searchText = string.Empty;
  private          bool                 _isSearchActive;

  public string Title => "EarthTool PAR Editor" + (string.IsNullOrEmpty(FilePath) ? string.Empty : $" [{FilePath}]");

  public string TotalEntities => _totalEntries == 0 ? string.Empty : $"Total Parameter Entries: {_totalEntries}";

  public string? FilePath
  {
    get => _filePath;
    set => this.RaiseAndSetIfChanged(ref _filePath, value);
  }

  public HierarchicalTreeDataGridSource<ParameterTreeNode> ParameterTree { get; }

  public ObservableCollection<ParameterTreeNode> Parameters { get; }

  public object? SelectedItem
  {
    get => _selectedItem;
    set => this.RaiseAndSetIfChanged(ref _selectedItem, value);
  }
  
  public bool HasUnsavedChanges
  {
    get => _hasUnsavedChanges;
    set => this.RaiseAndSetIfChanged(ref _hasUnsavedChanges, value);
  }

  public string SearchText
  {
    get => _searchText;
    set
    {
      this.RaiseAndSetIfChanged(ref _searchText, value);
      PerformSearch();
    }
  }

  public bool IsSearchActive
  {
    get => _isSearchActive;
    set => this.RaiseAndSetIfChanged(ref _isSearchActive, value);
  }

  public bool CanSave => _currentParFile != null;
  public bool CanDeleteEntity => SelectedItem is ParameterTreeNode node && node.Entity != null;

  public ICommand OpenFileCommand { get; }
  public ICommand SaveFileCommand { get; }
  public ICommand SaveAsFileCommand { get; }
  public ICommand ClearSearchCommand { get; }
  public ICommand AddEntityCommand { get; }
  public ICommand DeleteEntityCommand { get; }

  public Interaction<Unit, string?> OpenFileDialog { get; }
  public Interaction<Unit, string?> SaveFileDialog { get; }

  public MainWindowViewModel(IReader<ParFile> reader, IWriter<ParFile> writer, ParameterTreeBuilder treeBuilder)
  {
    _reader = reader;
    _writer = writer;
    _treeBuilder = treeBuilder;

    OpenFileDialog = new Interaction<Unit, string?>();
    SaveFileDialog = new Interaction<Unit, string?>();
    Parameters = new ObservableCollection<ParameterTreeNode>();
    ParameterTree = new HierarchicalTreeDataGridSource<ParameterTreeNode>(Parameters)
    {
      Columns =
      {
        new HierarchicalExpanderColumn<ParameterTreeNode>(
          new TextColumn<ParameterTreeNode, string>("Name", x => x.Name),
          x => x.Children,
          x => x.HasChildren)
      },
    };
    ParameterTree.RowSelection.SelectionChanged += OnRowSelectionChanged;

    // Create commands after object is fully initialized
    OpenFileCommand = CreateOpenFileCommand();
    SaveFileCommand = CreateSaveFileCommand();
    SaveAsFileCommand = CreateSaveAsFileCommand();
    ClearSearchCommand = CreateClearSearchCommand();
    AddEntityCommand = CreateAddEntityCommand();
    DeleteEntityCommand = CreateDeleteEntityCommand();
    
    InitializeCanExecuteObservables();
  }

  private void InitializeCanExecuteObservables()
  {
    // Set up CanExecute conditions for commands that need them
    var hasFileObservable = this.WhenAnyValue(x => x._currentParFile).Select(x => x != null);
    var hasSelectedEntityObservable = this.WhenAnyValue(x => x.SelectedItem)
      .Select(x => x is ParameterTreeNode node && node.Entity != null);

    // These will be used later when we implement proper conditional commands
    // For now, commands are always enabled to avoid the initialization error
  }

  private void OnRowSelectionChanged(object? sender, TreeSelectionModelSelectionChangedEventArgs<ParameterTreeNode> e)
  {
    SelectedItem = e.SelectedItems.SingleOrDefault();
    this.RaisePropertyChanged(nameof(CanDeleteEntity));
  }

  private ReactiveCommand<Unit, Unit> CreateOpenFileCommand()
    => ReactiveCommand.CreateFromTask(async () =>
    {
      var filePath = await OpenFileDialog.Handle(Unit.Default);

      if (filePath != null)
      {
        FilePath = filePath;
        this.RaisePropertyChanged(nameof(Title));

        _currentParFile = _reader.Read(filePath);

        Parameters.Clear();
        Parameters.AddRange(_treeBuilder.WithResearch(_currentParFile.Research.Select(r => r.ToViewModel()))
          .WithEntityGroups(_currentParFile.Groups.Select(g => g.ToViewModel()))
          .Build(true));
        _totalEntries = _currentParFile.Research.Count() + _currentParFile.Groups.Sum(g => g.Entities.Count());
        this.RaisePropertyChanged(nameof(TotalEntities));
        this.RaisePropertyChanged(nameof(CanSave));
        HasUnsavedChanges = false;
      }
    });

  private ReactiveCommand<Unit, Unit> CreateSaveFileCommand()
    => ReactiveCommand.CreateFromTask(async () =>
    {
      if (_currentParFile == null || string.IsNullOrEmpty(FilePath))
      {
        await SaveAsFile();
        return;
      }

      await SaveToFile(FilePath);
    });

  private ReactiveCommand<Unit, Unit> CreateSaveAsFileCommand()
    => ReactiveCommand.CreateFromTask(async () =>
    {
      await SaveAsFile();
    });

  private async System.Threading.Tasks.Task SaveAsFile()
  {
    var filePath = await SaveFileDialog.Handle(Unit.Default);
    if (filePath != null)
    {
      FilePath = filePath;
      this.RaisePropertyChanged(nameof(Title));
      await SaveToFile(filePath);
    }
  }

  private async System.Threading.Tasks.Task SaveToFile(string filePath)
  {
    if (_currentParFile == null) return;

    try
    {
      SyncViewModelsToModels();
      await System.Threading.Tasks.Task.Run(() => _writer.Write(_currentParFile, filePath));
      HasUnsavedChanges = false;
    }
    catch (System.Exception ex)
    {
      // TODO: Add proper error handling and user notification
      System.Diagnostics.Debug.WriteLine($"Error saving file: {ex.Message}");
    }
  }

  private void SyncViewModelsToModels()
  {
    // TODO: Implement synchronization from ViewModels back to original models
    // This is a complex task that requires traversing the tree and updating
    // the original ParFile structure with changes from ViewModels
  }

  private ReactiveCommand<Unit, Unit> CreateClearSearchCommand()
    => ReactiveCommand.Create(() =>
    {
      SearchText = string.Empty;
      IsSearchActive = false;
      RefreshParameterTree();
    });

  private void PerformSearch()
  {
    if (string.IsNullOrWhiteSpace(SearchText))
    {
      IsSearchActive = false;
      RefreshParameterTree();
      return;
    }

    IsSearchActive = true;
    RefreshParameterTree();
  }

  private void RefreshParameterTree()
  {
    if (_currentParFile == null) return;

    var research = _currentParFile.Research.Select(r => r.ToViewModel());
    var entityGroups = _currentParFile.Groups.Select(g => g.ToViewModel());

    if (IsSearchActive && !string.IsNullOrWhiteSpace(SearchText))
    {
      research = research.Where(r => ContainsSearchText(r));
      entityGroups = entityGroups.Where(eg => ContainsSearchText(eg) || 
        eg.Entities.Any(e => ContainsSearchText(e)));
    }

    Parameters.Clear();
    Parameters.AddRange(_treeBuilder.WithResearch(research)
      .WithEntityGroups(entityGroups)
      .Build(true));
  }

  private bool ContainsSearchText(object? obj)
  {
    if (obj == null || string.IsNullOrWhiteSpace(SearchText)) return false;

    var searchLower = SearchText.ToLowerInvariant();
    
    // Search by name property if it exists
    var nameProperty = obj.GetType().GetProperty("Name");
    if (nameProperty?.GetValue(obj) is string name)
    {
      return name.ToLowerInvariant().Contains(searchLower);
    }

    return false;
  }

  private ReactiveCommand<Unit, Unit> CreateAddEntityCommand()
    => ReactiveCommand.Create(() =>
    {
      // TODO: Show dialog to select entity type and create new entity
      // For now, just show placeholder functionality
      System.Diagnostics.Debug.WriteLine("Add Entity command triggered");
      HasUnsavedChanges = true;
    });

  private ReactiveCommand<Unit, Unit> CreateDeleteEntityCommand()
    => ReactiveCommand.Create(() =>
    {
      if (SelectedItem is ParameterTreeNode node && node.Entity != null)
      {
        // TODO: Implement actual deletion from the data structure
        System.Diagnostics.Debug.WriteLine($"Delete Entity: {node.Name}");
        HasUnsavedChanges = true;
        RefreshParameterTree();
      }
    });
}