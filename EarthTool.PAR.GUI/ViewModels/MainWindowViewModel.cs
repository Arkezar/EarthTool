using EarthTool.PAR.GUI.Services;
using EarthTool.PAR.Models;
using Microsoft.Extensions.Logging;
using ReactiveUI;
using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Reactive;
using System.Reactive.Linq;
using System.Threading.Tasks;

namespace EarthTool.PAR.GUI.ViewModels;

/// <summary>
/// Main ViewModel for the PAR Editor.
/// </summary>
public class MainWindowViewModel : ViewModelBase, IDisposable
{
  private readonly IParFileService _parFileService;
  private readonly IDialogService _dialogService;
  private readonly INotificationService _notificationService;
  private readonly IUndoRedoService _undoRedoService;
  private readonly IEntityValidationService _validationService;
  private readonly ILogger<MainWindowViewModel> _logger;
  private readonly EntityDetailsViewModel _entityDetailsViewModel;

  private ParFile? _currentParFile;
  private string? _currentFilePath;
  private bool _hasUnsavedChanges;
  private bool _isBusy;
  private string _statusMessage = "Ready";
  private string _searchText = string.Empty;
  private TreeNodeViewModelBase? _selectedNode;
  private TreeNodeViewModelBase? _selectedEntity;

  public MainWindowViewModel(
    IParFileService parFileService,
    IDialogService dialogService,
    INotificationService notificationService,
    IUndoRedoService undoRedoService,
    IEntityValidationService validationService,
    ILogger<MainWindowViewModel> logger,
    EntityDetailsViewModel entityDetailsViewModel)
  {
    _parFileService = parFileService ?? throw new ArgumentNullException(nameof(parFileService));
    _dialogService = dialogService ?? throw new ArgumentNullException(nameof(dialogService));
    _notificationService = notificationService ?? throw new ArgumentNullException(nameof(notificationService));
    _undoRedoService = undoRedoService ?? throw new ArgumentNullException(nameof(undoRedoService));
    _validationService = validationService ?? throw new ArgumentNullException(nameof(validationService));
    _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    _entityDetailsViewModel = entityDetailsViewModel ?? throw new ArgumentNullException(nameof(entityDetailsViewModel));

    RootNodes = new ObservableCollection<TreeNodeViewModelBase>();

    InitializeCommands();
    InitializeSearchDebounce();

    _logger.LogInformation("ParEditorViewModel initialized");
  }

  #region Properties

  /// <summary>
  /// Gets the collection of root nodes for the main tree (Entity Groups + Research).
  /// </summary>
  public ObservableCollection<TreeNodeViewModelBase> RootNodes { get; }

  /// <summary>
  /// Gets or sets the currently selected tree node.
  /// </summary>
  public TreeNodeViewModelBase? SelectedNode
  {
    get => _selectedNode;
    set
    {
      if (_selectedNode == value)
        return;

      _selectedNode = value;
      this.RaisePropertyChanged();

      // Update SelectedEntity/SelectedResearch based on node type
      if (value is EntityListItemViewModel entityItem)
      {
        SelectedEntity = entityItem;
      }
      else if (value is ResearchViewModel researchItem)
      {
        SelectedEntity = researchItem;
      }
      else
      {
        SelectedEntity = null;
      }
    }
  }

  /// <summary>
  /// Gets or sets the currently selected entity.
  /// </summary>
  public TreeNodeViewModelBase? SelectedEntity
  {
    get => _selectedEntity;
    set
    {
      if (_selectedEntity == value)
        return;

      _selectedEntity = value;
      this.RaisePropertyChanged();

      // Update EntityDetailsViewModel
      if (value is EntityListItemViewModel entityItem)
      {
        _entityDetailsViewModel.CurrentEntity = entityItem?.EditableEntity;
        _entityDetailsViewModel.CurrentResearch = null;
      }
      else if (value is ResearchViewModel researchItem)
      {
        _entityDetailsViewModel.CurrentEntity = null;
        _entityDetailsViewModel.CurrentResearch = researchItem?.EditableResearch;
      }
      else
      {
        _entityDetailsViewModel.CurrentEntity = null;
        _entityDetailsViewModel.CurrentResearch = null;
      }
    }
  }

  /// <summary>
  /// Gets the current PAR file.
  /// </summary>
  public ParFile? CurrentParFile => _currentParFile;

  /// <summary>
  /// Gets the current file path.
  /// </summary>
  public string? CurrentFilePath => _currentFilePath;

  /// <summary>
  /// Gets whether a file is currently open.
  /// </summary>
  public bool IsFileOpen => _currentParFile != null;

  /// <summary>
  /// Gets or sets whether there are unsaved changes.
  /// </summary>
  public bool HasUnsavedChanges
  {
    get => _hasUnsavedChanges;
    set => this.RaiseAndSetIfChanged(ref _hasUnsavedChanges, value);
  }

  /// <summary>
  /// Gets or sets whether the editor is busy.
  /// </summary>
  public bool IsBusy
  {
    get => _isBusy;
    set => this.RaiseAndSetIfChanged(ref _isBusy, value);
  }

  /// <summary>
  /// Gets or sets the status message.
  /// </summary>
  public string StatusMessage
  {
    get => _statusMessage;
    set => this.RaiseAndSetIfChanged(ref _statusMessage, value);
  }

  /// <summary>
  /// Gets or sets the search text for filtering entities.
  /// </summary>
  public string SearchText
  {
    get => _searchText;
    set => this.RaiseAndSetIfChanged(ref _searchText, value);
  }

  /// <summary>
  /// Gets the window title.
  /// </summary>
  public string WindowTitle
  {
    get
    {
      var title = "PAR Editor";
      if (!string.IsNullOrEmpty(_currentFilePath))
      {
        var fileName = System.IO.Path.GetFileName(_currentFilePath);
        title = $"{fileName}{(HasUnsavedChanges ? "*" : "")} - {title}";
      }
      return title;
    }
  }

  /// <summary>
  /// Gets whether undo is available.
  /// </summary>
  public bool CanUndo => _undoRedoService.CanUndo;

  /// <summary>
  /// Gets whether redo is available.
  /// </summary>
  public bool CanRedo => _undoRedoService.CanRedo;

  /// <summary>
  /// Gets the entity details ViewModel.
  /// </summary>
  public EntityDetailsViewModel EntityDetailsViewModel => _entityDetailsViewModel;
  
  #endregion

  #region Commands

  public ReactiveCommand<Unit, Unit> NewFileCommand { get; private set; } = null!;
  public ReactiveCommand<Unit, Unit> OpenFileCommand { get; private set; } = null!;
  public ReactiveCommand<Unit, Unit> SaveFileCommand { get; private set; } = null!;
  public ReactiveCommand<Unit, Unit> SaveAsCommand { get; private set; } = null!;
  public ReactiveCommand<Unit, Unit> CloseFileCommand { get; private set; } = null!;
  public ReactiveCommand<Unit, Unit> UndoCommand { get; private set; } = null!;
  public ReactiveCommand<Unit, Unit> RedoCommand { get; private set; } = null!;
  public ReactiveCommand<Unit, Unit> AddEntityCommand { get; private set; } = null!;
  public ReactiveCommand<Unit, Unit> CloneEntityCommand { get; private set; } = null!;
  public ReactiveCommand<Unit, Unit> DeleteEntityCommand { get; private set; } = null!;
  public ReactiveCommand<Unit, Unit> CopyEntityCommand { get; private set; } = null!;
  public ReactiveCommand<Unit, Unit> PasteEntityCommand { get; private set; } = null!;

  private void InitializeCommands()
  {
    // File commands
    NewFileCommand = ReactiveCommand.CreateFromTask(NewFileAsync);
    OpenFileCommand = ReactiveCommand.CreateFromTask(OpenFileAsync);

    var canSave = this.WhenAnyValue(
      x => x.IsFileOpen,
      x => x.HasUnsavedChanges,
      (isOpen, hasChanges) => isOpen && hasChanges);
    SaveFileCommand = ReactiveCommand.CreateFromTask(SaveFileAsync, canSave);

    var canSaveAs = this.WhenAnyValue(x => x.IsFileOpen);
    SaveAsCommand = ReactiveCommand.CreateFromTask(SaveAsAsync, canSaveAs);
    CloseFileCommand = ReactiveCommand.CreateFromTask(CloseFileAsync, canSaveAs);

    // Edit commands
    var canUndo = this.WhenAnyValue(x => x.CanUndo);
    UndoCommand = ReactiveCommand.Create(Undo, canUndo);

    var canRedo = this.WhenAnyValue(x => x.CanRedo);
    RedoCommand = ReactiveCommand.Create(Redo, canRedo);

    // Entity commands
    var canAddEntity = this.WhenAnyValue(x => x.IsFileOpen);
    AddEntityCommand = ReactiveCommand.CreateFromTask(AddEntityAsync, canAddEntity);

    var canModifyEntity = this.WhenAnyValue(
      x => x.IsFileOpen,
      x => x.SelectedEntity,
      (isOpen, entity) => isOpen && entity != null);
    CloneEntityCommand = ReactiveCommand.CreateFromTask(CloneEntityAsync, canModifyEntity);
    DeleteEntityCommand = ReactiveCommand.CreateFromTask(DeleteEntityAsync, canModifyEntity);
    CopyEntityCommand = ReactiveCommand.Create(CopyEntity, canModifyEntity);
    PasteEntityCommand = ReactiveCommand.CreateFromTask(PasteEntityAsync, canAddEntity);

    // Subscribe to property changes for WindowTitle
    this.WhenAnyValue(x => x.HasUnsavedChanges, x => x.CurrentFilePath)
      .Subscribe(_ => this.RaisePropertyChanged(nameof(WindowTitle)));
  }

  private void InitializeSearchDebounce()
  {
    // Apply filter with 200ms debounce when SearchText changes
    this.WhenAnyValue(x => x.SearchText)
      .Throttle(TimeSpan.FromMilliseconds(200))
      .ObserveOn(RxApp.MainThreadScheduler)
      .Subscribe(_ => ApplyFilter());
  }

  #endregion

  #region Command Implementations

  private async Task NewFileAsync()
  {
    try
    {
      if (!await PromptSaveChangesAsync())
        return;

      _logger.LogInformation("Creating new PAR file");
      IsBusy = true;
      StatusMessage = "Creating new file...";

      _currentParFile = await _parFileService.CreateNewAsync();
      _currentFilePath = null;
      HasUnsavedChanges = true;

      LoadParFile();

      _notificationService.ShowSuccess("New PAR file created");
      StatusMessage = "New file ready";
      _logger.LogInformation("Created new PAR file");
    }
    catch (Exception ex)
    {
      _logger.LogError(ex, "Failed to create new PAR file");
      _notificationService.ShowError("Failed to create new file", ex);
      StatusMessage = "Failed to create file";
    }
    finally
    {
      IsBusy = false;
    }
  }

  private async Task OpenFileAsync()
  {
    try
    {
      if (!await PromptSaveChangesAsync())
        return;

      var filePath = await _dialogService.ShowOpenFileDialogAsync("Open PAR File", ("PAR Files", "*.par"));
      if (string.IsNullOrEmpty(filePath))
        return;

      _logger.LogInformation("Opening PAR file: {FilePath}", filePath);
      IsBusy = true;
      StatusMessage = "Loading file...";

      _currentParFile = await _parFileService.LoadAsync(filePath);
      _currentFilePath = filePath;
      HasUnsavedChanges = false;

      LoadParFile();

      _notificationService.ShowSuccess($"Opened {System.IO.Path.GetFileName(filePath)}");

      var entityGroupsRoot = RootNodes.OfType<EntityGroupsRootNodeViewModel>().FirstOrDefault();
      var researchRoot = RootNodes.OfType<ResearchRootNodeViewModel>().FirstOrDefault();

      int totalEntities = entityGroupsRoot?.Factions
        .SelectMany(f => f.GroupTypes)
        .SelectMany(gt => gt.EntityGroups)
        .Sum(eg => eg.Entities.Count) ?? 0;
      int totalResearch = researchRoot?.ChildCount ?? 0;

      StatusMessage = $"Loaded {totalEntities} entities and {totalResearch} research items";
      _logger.LogInformation("Opened PAR file with {EntityCount} entities, {ResearchCount} research",
        totalEntities, totalResearch);
    }
    catch (Exception ex)
    {
      _logger.LogError(ex, "Failed to open PAR file");
      _notificationService.ShowError("Failed to open file", ex);
      StatusMessage = "Failed to open file";
    }
    finally
    {
      IsBusy = false;
    }
  }

  private async Task SaveFileAsync()
  {
    if (_currentParFile == null)
      return;

    try
    {
      if (string.IsNullOrEmpty(_currentFilePath))
      {
        await SaveAsAsync();
        return;
      }

      _logger.LogInformation("Saving PAR file: {FilePath}", _currentFilePath);
      IsBusy = true;
      StatusMessage = "Saving file...";

      await _parFileService.SaveAsync(_currentParFile, _currentFilePath);

      // Accept changes on all modified entities to clear dirty flags
      AcceptAllEntityChanges();

      HasUnsavedChanges = false;
      _notificationService.ShowSuccess($"Saved {System.IO.Path.GetFileName(_currentFilePath)}");
      StatusMessage = "File saved";
      _logger.LogInformation("Saved PAR file");
    }
    catch (Exception ex)
    {
      _logger.LogError(ex, "Failed to save PAR file");
      _notificationService.ShowError("Failed to save file", ex);
      StatusMessage = "Failed to save file";
    }
    finally
    {
      IsBusy = false;
    }
  }

  private async Task SaveAsAsync()
  {
    if (_currentParFile == null)
      return;

    try
    {
      var defaultFileName = !string.IsNullOrEmpty(_currentFilePath)
        ? System.IO.Path.GetFileName(_currentFilePath)
        : "parameters.par";

      var filePath = await _dialogService.ShowSaveFileDialogAsync(defaultFileName);
      if (string.IsNullOrEmpty(filePath))
        return;

      _logger.LogInformation("Saving PAR file as: {FilePath}", filePath);
      IsBusy = true;
      StatusMessage = "Saving file...";

      await _parFileService.SaveAsync(_currentParFile, filePath);

      // Accept changes on all modified entities to clear dirty flags
      AcceptAllEntityChanges();

      _currentFilePath = filePath;
      HasUnsavedChanges = false;
      _notificationService.ShowSuccess($"Saved as {System.IO.Path.GetFileName(filePath)}");
      StatusMessage = "File saved";
      _logger.LogInformation("Saved PAR file as new file");
    }
    catch (Exception ex)
    {
      _logger.LogError(ex, "Failed to save PAR file");
      _notificationService.ShowError("Failed to save file", ex);
      StatusMessage = "Failed to save file";
    }
    finally
    {
      IsBusy = false;
    }
  }

  private async Task CloseFileAsync()
  {
    try
    {
      if (!await PromptSaveChangesAsync())
        return;

      _logger.LogInformation("Closing PAR file");

      _currentParFile = null;
      _currentFilePath = null;
      HasUnsavedChanges = false;

      RootNodes.Clear();
      SelectedNode = null;
      SelectedEntity = null;
      _undoRedoService.Clear();

      StatusMessage = "File closed";
      this.RaisePropertyChanged(nameof(IsFileOpen));
      _logger.LogInformation("Closed PAR file");
    }
    catch (Exception ex)
    {
      _logger.LogError(ex, "Failed to close PAR file");
      _notificationService.ShowError("Failed to close file", ex);
    }
  }

  private void Undo()
  {
    try
    {
      _undoRedoService.Undo();
      this.RaisePropertyChanged(nameof(CanUndo));
      this.RaisePropertyChanged(nameof(CanRedo));
      _logger.LogDebug("Undo executed");
    }
    catch (Exception ex)
    {
      _logger.LogError(ex, "Failed to undo");
      _notificationService.ShowError("Failed to undo", ex);
    }
  }

  private void Redo()
  {
    try
    {
      _undoRedoService.Redo();
      this.RaisePropertyChanged(nameof(CanUndo));
      this.RaisePropertyChanged(nameof(CanRedo));
      _logger.LogDebug("Redo executed");
    }
    catch (Exception ex)
    {
      _logger.LogError(ex, "Failed to redo");
      _notificationService.ShowError("Failed to redo", ex);
    }
  }

  private Task AddEntityAsync()
  {
    // TODO: Implement in Sprint 7
    _notificationService.ShowInfo("Add Entity - Coming soon");
    return Task.CompletedTask;
  }

  private Task CloneEntityAsync()
  {
    // TODO: Implement in Sprint 7
    _notificationService.ShowInfo("Clone Entity - Coming soon");
    return Task.CompletedTask;
  }

  private Task DeleteEntityAsync()
  {
    // TODO: Implement in Sprint 7
    _notificationService.ShowInfo("Delete Entity - Coming soon");
    return Task.CompletedTask;
  }

  private void CopyEntity()
  {
    // TODO: Implement in Sprint 7
    _notificationService.ShowInfo("Copy Entity - Coming soon");
  }

  private Task PasteEntityAsync()
  {
    // TODO: Implement in Sprint 7
    _notificationService.ShowInfo("Paste Entity - Coming soon");
    return Task.CompletedTask;
  }

  #endregion

  #region Helper Methods

  private void LoadParFile()
  {
    if (_currentParFile == null)
      return;

    RootNodes.Clear();

    // Set validation context
    _validationService.SetContext(_currentParFile);

    // Build Entity Groups hierarchy: Root -> Faction -> GroupType -> EntityGroup -> Entity
    var entityGroupsRoot = new EntityGroupsRootNodeViewModel();

    var factionGroups = _currentParFile.Groups
      .GroupBy(g => g.Faction)
      .OrderBy(fg => fg.Key);

    foreach (var factionGroup in factionGroups)
    {
      var factionNode = new FactionNodeViewModel(factionGroup.Key);

      var groupTypeGroups = factionGroup
        .GroupBy(g => g.GroupType)
        .OrderBy(gtg => gtg.Key);

      foreach (var groupTypeGroup in groupTypeGroups)
      {
        var groupTypeNode = new GroupTypeNodeViewModel(groupTypeGroup.Key);

        var sortedEntityGroups = groupTypeGroup
          .OrderBy(eg => eg.Name);

        foreach (var entityGroup in sortedEntityGroups)
        {
          var entityGroupNode = new EntityGroupNodeViewModel(entityGroup);
          
          // Subscribe to IsDirty changes for each entity
          foreach (var entityItem in entityGroupNode.Entities)
          {
            entityItem.EditableEntity.WhenAnyValue(e => e.IsDirty)
              .Subscribe(_ => UpdateHasUnsavedChanges());
          }
          
          groupTypeNode.EntityGroups.Add(entityGroupNode);
        }

        factionNode.GroupTypes.Add(groupTypeNode);
      }

      entityGroupsRoot.Factions.Add(factionNode);
    }

    RootNodes.Add(entityGroupsRoot);

    // Build Research hierarchy: Root -> Faction -> ResearchType -> Research
    var researchRoot = new ResearchRootNodeViewModel();

    var researchFactionGroups = _currentParFile.Research
      .GroupBy(r => r.Faction)
      .OrderBy(fg => fg.Key);

    foreach (var researchFactionGroup in researchFactionGroups)
    {
      var factionResearchNode = new FactionResearchNodeViewModel(researchFactionGroup.Key);

      var researchTypeGroups = researchFactionGroup
        .GroupBy(r => r.Type)
        .OrderBy(rtg => rtg.Key);

      foreach (var researchTypeGroup in researchTypeGroups)
      {
        var researchTypeNode = new ResearchTypeNodeViewModel(researchTypeGroup.Key);

        var sortedResearch = researchTypeGroup
          .OrderBy(r => r.Name);

        foreach (var research in sortedResearch)
        {
          var researchVm = new ResearchViewModel(research);
          
          // Subscribe to IsDirty changes for each research
          researchVm.EditableResearch.WhenAnyValue(r => r.IsDirty)
            .Subscribe(_ => UpdateHasUnsavedChanges());
          
          researchTypeNode.ResearchItems.Add(researchVm);
        }

        factionResearchNode.ResearchTypes.Add(researchTypeNode);
      }

      researchRoot.Factions.Add(factionResearchNode);
    }

    RootNodes.Add(researchRoot);

    // Set ParFile context in EntityDetailsViewModel for research lookups
    _entityDetailsViewModel.ParFile = _currentParFile;
    _entityDetailsViewModel.NavigateToResearch = (researchName) => NavigateToResearch(researchName);

    this.RaisePropertyChanged(nameof(IsFileOpen));
    _logger.LogDebug("Loaded PAR file: {EntityGroupCount} entity groups, {ResearchCount} research",
      entityGroupsRoot.ChildCount, researchRoot.ChildCount);
  }

  private void ApplyFilter()
  {
    foreach (var rootNode in RootNodes)
    {
      rootNode.ApplyFilter(_searchText);
    }
  }

  private void UpdateHasUnsavedChanges()
  {
    // Check if any entity has unsaved changes
    var entityGroupsRoot = RootNodes.OfType<EntityGroupsRootNodeViewModel>().FirstOrDefault();
    var hasAnyDirtyEntity = entityGroupsRoot?.Factions
      .SelectMany(f => f.GroupTypes)
      .SelectMany(gt => gt.EntityGroups)
      .SelectMany(eg => eg.Entities)
      .Any(e => e.IsDirty) ?? false;

    // Check if any research has unsaved changes
    var researchRoot = RootNodes.OfType<ResearchRootNodeViewModel>().FirstOrDefault();
    var hasAnyDirtyResearch = researchRoot?.Factions
      .SelectMany(f => f.ResearchTypes)
      .SelectMany(t => t.ResearchItems)
      .Any(r => r.IsDirty) ?? false;

    HasUnsavedChanges = hasAnyDirtyEntity || hasAnyDirtyResearch;
  }

  private void AcceptAllEntityChanges()
  {
    // Accept changes on all entities to clear dirty flags and update baseline
    var entityGroupsRoot = RootNodes.OfType<EntityGroupsRootNodeViewModel>().FirstOrDefault();
    if (entityGroupsRoot != null)
    {
      var allEntities = entityGroupsRoot.Factions
        .SelectMany(f => f.GroupTypes)
        .SelectMany(gt => gt.EntityGroups)
        .SelectMany(eg => eg.Entities)
        .Select(e => e.EditableEntity);

      foreach (var entity in allEntities)
      {
        if (entity.IsDirty)
        {
          entity.AcceptChanges();
          _logger.LogDebug("Accepted changes for entity '{Name}'", entity.DisplayName);
        }
      }
    }

    // Accept changes on all research to clear dirty flags and update baseline
    var researchRoot = RootNodes.OfType<ResearchRootNodeViewModel>().FirstOrDefault();
    if (researchRoot != null)
    {
      var allResearch = researchRoot.Factions
        .SelectMany(f => f.ResearchTypes)
        .SelectMany(t => t.ResearchItems);

      foreach (var research in allResearch)
      {
        if (research.IsDirty)
        {
          research.EditableResearch.AcceptChanges();
          _logger.LogDebug("Accepted changes for research '{Name}'", research.DisplayName);
        }
      }
    }

    _logger.LogInformation("Accepted changes for all modified entities and research");
  }

  private async Task<bool> PromptSaveChangesAsync()
  {
    if (!HasUnsavedChanges)
      return true;

    var result = await _dialogService.ShowMessageBoxAsync(
      "The current file has unsaved changes. Do you want to save them?",
      "Unsaved Changes",
      MessageBoxType.YesNoCancel);

    return result switch
    {
      MessageBoxResult.Yes => await TrySaveFileAsync(),
      MessageBoxResult.No => true,
      MessageBoxResult.Cancel => false,
      _ => false
    };
  }

  private async Task<bool> TrySaveFileAsync()
  {
    try
    {
      await SaveFileAsync();
      return !HasUnsavedChanges; // Only return true if save was successful
    }
    catch
    {
      return false;
    }
  }

  #endregion

  #region IDisposable

  public void Dispose()
  {
    _undoRedoService.Clear();
    GC.SuppressFinalize(this);
  }

  #endregion

  #region Entity Navigation

  /// <summary>
  /// Finds and selects an entity by name in the tree.
  /// </summary>
  public bool NavigateToEntity(string entityName)
  {
    if (string.IsNullOrEmpty(entityName))
      return false;

    _logger.LogInformation("Searching for entity '{EntityName}' in tree...", entityName);

    foreach (var rootNode in RootNodes)
    {
      var foundNode = FindEntityInNode(rootNode, entityName);
      if (foundNode != null)
      {
        _logger.LogInformation("Found entity '{EntityName}', selecting node", entityName);
        ExpandPathToNode(foundNode);
        SelectedNode = foundNode;
        return true;
      }
    }

    _logger.LogWarning("Entity '{EntityName}' not found in tree", entityName);
    return false;
  }

  private TreeNodeViewModelBase? FindEntityInNode(TreeNodeViewModelBase node, string entityName)
  {
    if (node is EntityListItemViewModel entityItem && 
        entityItem.Name.Equals(entityName, StringComparison.OrdinalIgnoreCase))
      return node;

    if (node.Children != null)
    {
      foreach (var child in node.Children)
      {
        var found = FindEntityInNode(child, entityName);
        if (found != null)
        {
          // Expand this node as it's on the path to target
          if (!node.IsExpanded)
          {
            node.IsExpanded = true;
            _logger.LogDebug("Expanded node '{NodeName}' on path to target", node.DisplayName);
          }
          return found;
        }
      }
    }

    return null;
  }

  private void ExpandPathToNode(TreeNodeViewModelBase targetNode)
  {
    // The FindEntityInNode already expands the path
    // This method is now a no-op but kept for clarity
  }

  /// <summary>
  /// Finds and selects a research by name in the tree.
  /// </summary>
  public bool NavigateToResearch(string researchName)
  {
    if (string.IsNullOrEmpty(researchName))
      return false;

    _logger.LogInformation("Searching for research '{ResearchName}' in tree...", researchName);

    foreach (var rootNode in RootNodes)
    {
      var foundNode = FindResearchInNode(rootNode, researchName);
      if (foundNode != null)
      {
        _logger.LogInformation("Found research '{ResearchName}', selecting node", researchName);
        SelectedNode = foundNode;
        return true;
      }
    }

    _logger.LogWarning("Research '{ResearchName}' not found in tree", researchName);
    return false;
  }

  private TreeNodeViewModelBase? FindResearchInNode(TreeNodeViewModelBase node, string researchName)
  {
    if (node is ResearchViewModel researchItem && 
        researchItem.Name.Equals(researchName, StringComparison.OrdinalIgnoreCase))
      return node;

    if (node.Children != null)
    {
      foreach (var child in node.Children)
      {
        var found = FindResearchInNode(child, researchName);
        if (found != null)
        {
          // Expand this node as it's on the path to target
          if (!node.IsExpanded)
          {
            node.IsExpanded = true;
            _logger.LogDebug("Expanded node '{NodeName}' on path to target", node.DisplayName);
          }
          return found;
        }
      }
    }

    return null;
  }

  #endregion
}
