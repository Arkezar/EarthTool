using Avalonia.Threading;
using EarthTool.Common.Interfaces;
using EarthTool.WD.GUI.Services;
using Microsoft.Extensions.Logging;
using ReactiveUI;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Reactive;
using System.Reactive.Concurrency;
using System.Reactive.Linq;
using System.Threading.Tasks;

namespace EarthTool.WD.GUI.ViewModels;

/// <summary>
/// Main ViewModel for the WD Archive Manager application.
/// </summary>
public class MainWindowViewModel : ViewModelBase, IDisposable
{
  private readonly IArchiver                    _archiver;
  private readonly IDialogService               _dialogService;
  private readonly INotificationService         _notificationService;
  private readonly ILogger<MainWindowViewModel> _logger;

  private IArchive? _currentArchive;
  private string?   _currentFilePath;
  private bool      _hasUnsavedChanges;
  private bool      _isBusy;
  private string    _statusMessage = "Ready";

  public MainWindowViewModel(
    IArchiver archiver,
    IDialogService dialogService,
    INotificationService notificationService,
    ILogger<MainWindowViewModel> logger)
  {
    _archiver = archiver                       ?? throw new ArgumentNullException(nameof(archiver));
    _dialogService = dialogService             ?? throw new ArgumentNullException(nameof(dialogService));
    _notificationService = notificationService ?? throw new ArgumentNullException(nameof(notificationService));
    _logger = logger                           ?? throw new ArgumentNullException(nameof(logger));

    ArchiveItems = new ObservableCollection<ArchiveItemViewModel>();
    ArchiveInfo = new ArchiveInfoViewModel();

    // Subscribe to notification service
    _notificationService.NotificationRaised += OnNotificationRaised;

    InitializeCommands();
  }

  #region Properties

  public ObservableCollection<ArchiveItemViewModel> ArchiveItems { get; }
  public ArchiveInfoViewModel ArchiveInfo { get; }

  // For single selection scenarios
  private ArchiveItemViewModel? _selectedItem;

  public ArchiveItemViewModel? SelectedItem
  {
    get => _selectedItem;
    set
    {
      this.RaiseAndSetIfChanged(ref _selectedItem, value);
      this.RaisePropertyChanged(nameof(HasSelection));
    }
  }

  // Track whether we have a selection (for command CanExecute)
  public bool HasSelection => SelectedItem != null;

  public bool IsArchiveOpen => _currentArchive != null;

  public bool HasUnsavedChanges
  {
    get => _hasUnsavedChanges;
    set => this.RaiseAndSetIfChanged(ref _hasUnsavedChanges, value);
  }

  public bool IsBusy
  {
    get => _isBusy;
    set => this.RaiseAndSetIfChanged(ref _isBusy, value);
  }

  public string StatusMessage
  {
    get => _statusMessage;
    set => this.RaiseAndSetIfChanged(ref _statusMessage, value);
  }

  public string WindowTitle
  {
    get
    {
      var title = "EarthTool WD Archive Manager";
      if (!string.IsNullOrEmpty(_currentFilePath))
      {
        var fileName = Path.GetFileName(_currentFilePath);
        title = $"{fileName}{(HasUnsavedChanges ? "*" : "")} - {title}";
      }

      return title;
    }
  }

  #endregion

  #region Commands

  public ReactiveCommand<Unit, Unit> OpenArchiveCommand { get; private set; } = null!;
  public ReactiveCommand<Unit, Unit> CreateArchiveCommand { get; private set; } = null!;
  public ReactiveCommand<Unit, Unit> SaveArchiveCommand { get; private set; } = null!;
  public ReactiveCommand<Unit, Unit> SaveArchiveAsCommand { get; private set; } = null!;
  public ReactiveCommand<Unit, Unit> CloseArchiveCommand { get; private set; } = null!;
  public ReactiveCommand<Unit, Unit> ExtractSelectedCommand { get; private set; } = null!;
  public ReactiveCommand<Unit, Unit> ExtractAllCommand { get; private set; } = null!;
  public ReactiveCommand<Unit, Unit> AddFilesCommand { get; private set; } = null!;
  public ReactiveCommand<Unit, Unit> RemoveSelectedCommand { get; private set; } = null!;
  public ReactiveCommand<Unit, Unit> ExitCommand { get; private set; } = null!;

  private void InitializeCommands()
  {
    // OpenArchiveCommand - always enabled
    OpenArchiveCommand = ReactiveCommand.CreateFromTask(OpenArchiveAsync);

    // CreateArchiveCommand - always enabled
    CreateArchiveCommand = ReactiveCommand.CreateFromTask(CreateArchiveAsync);

    // SaveArchiveCommand - enabled when archive is open and has changes
    var canSave = this.WhenAnyValue(
      x => x.IsArchiveOpen,
      x => x.HasUnsavedChanges,
      (isOpen, hasChanges) => isOpen && hasChanges);
    SaveArchiveCommand = ReactiveCommand.CreateFromTask(SaveArchiveAsync, canSave);

    // SaveArchiveAsCommand - enabled when archive is open
    var canSaveAs = this.WhenAnyValue(x => x.IsArchiveOpen);
    SaveArchiveAsCommand = ReactiveCommand.CreateFromTask(SaveArchiveAsAsync, canSaveAs);

    // CloseArchiveCommand - enabled when archive is open
    CloseArchiveCommand = ReactiveCommand.CreateFromTask(CloseArchiveAsync, canSaveAs);

    // ExtractSelectedCommand - enabled when item is selected
    var canExtractSelected = this.WhenAnyValue(
      x => x.IsArchiveOpen,
      x => x.SelectedItem,
      (isOpen, item) => isOpen && item != null);
    ExtractSelectedCommand = ReactiveCommand.CreateFromTask(ExtractSelectedAsync, canExtractSelected);

    // ExtractAllCommand - enabled when archive is open
    ExtractAllCommand = ReactiveCommand.CreateFromTask(ExtractAllAsync, canSaveAs);

    // AddFilesCommand - enabled when archive is open
    AddFilesCommand = ReactiveCommand.CreateFromTask(AddFilesAsync, canSaveAs);

    // RemoveSelectedCommand - enabled when items are selected
    RemoveSelectedCommand = ReactiveCommand.CreateFromTask(RemoveSelectedAsync, canExtractSelected);

    // ExitCommand - always enabled
    ExitCommand = ReactiveCommand.Create(Exit);

    // Subscribe to property changes for WindowTitle
    this.WhenAnyValue(x => x.HasUnsavedChanges)
      .Subscribe(_ => this.RaisePropertyChanged(nameof(WindowTitle)));
  }

  #endregion

  #region Command Implementations

  private async Task OpenArchiveAsync()
  {
    try
    {
      // Check for unsaved changes
      if (!await PromptSaveChangesAsync())
        return;

      var filePath = await _dialogService.ShowOpenFileDialogAsync();
      if (string.IsNullOrEmpty(filePath))
        return;

      IsBusy = true;
      StatusMessage = "Opening archive...";

      IArchive? loadedArchive = null;

      _currentArchive?.Dispose();
      loadedArchive = _archiver.OpenArchive(filePath);

      _currentArchive = loadedArchive;
      _currentFilePath = filePath;

      _logger.LogInformation("Archive loaded, item count: {Count}", _currentArchive?.Items.Count ?? 0);

      this.RaisePropertyChanged(nameof(IsArchiveOpen));

      LoadArchiveItems();
      HasUnsavedChanges = false;

      _logger.LogInformation("After LoadArchiveItems, ArchiveItems.Count: {Count}", ArchiveItems.Count);

      _notificationService.ShowSuccess($"Archive opened: {Path.GetFileName(filePath)}");
      StatusMessage = $"Loaded {ArchiveItems.Count} file(s)";
      _logger.LogInformation("Opened archive: {FilePath}", filePath);
      this.RaisePropertyChanged(nameof(WindowTitle));
      this.RaisePropertyChanged(nameof(ArchiveItems));
      this.RaisePropertyChanged(nameof(ArchiveInfo));
    }
    catch (Exception ex)
    {
      _notificationService.ShowError("Failed to open archive", ex);
      StatusMessage = "Failed to open archive";
    }
    finally
    {
      IsBusy = false;
      this.RaisePropertyChanged(nameof(IsArchiveOpen));
    }
  }

  private async Task CreateArchiveAsync()
  {
    try
    {
      // Check for unsaved changes
      if (!await PromptSaveChangesAsync())
        return;

      IsBusy = true;
      StatusMessage = "Creating new archive...";

      IArchive? newArchive = null;
      await Task.Run(() =>
      {
        _currentArchive?.Dispose();
        newArchive = _archiver.CreateArchive();
      });

      _currentArchive = newArchive;
      _currentFilePath = null;
      this.RaisePropertyChanged(nameof(IsArchiveOpen));

      ArchiveItems.Clear();
      ArchiveInfo.UpdateFromArchive(_currentArchive, null);
      HasUnsavedChanges = true;

      _notificationService.ShowSuccess("New archive created");
      StatusMessage = "New archive ready";
      _logger.LogInformation("Created new archive");
      this.RaisePropertyChanged(nameof(WindowTitle));
    }
    catch (Exception ex)
    {
      _notificationService.ShowError("Failed to create archive", ex);
      StatusMessage = "Failed to create archive";
    }
    finally
    {
      IsBusy = false;
      this.RaisePropertyChanged(nameof(IsArchiveOpen));
    }
  }

  private async Task SaveArchiveAsync()
  {
    if (_currentArchive == null) return;

    try
    {
      // If no file path, use Save As
      if (string.IsNullOrEmpty(_currentFilePath))
      {
        await SaveArchiveAsAsync();
        return;
      }

      IsBusy = true;
      StatusMessage = "Saving archive...";

      var filePath = _currentFilePath;
      await Task.Run(() => _archiver.SaveArchive(_currentArchive, filePath));

      HasUnsavedChanges = false;
      _notificationService.ShowSuccess($"Archive saved: {Path.GetFileName(filePath)}");
      StatusMessage = "Archive saved";
      _logger.LogInformation("Saved archive: {FilePath}", filePath);
    }
    catch (Exception ex)
    {
      _notificationService.ShowError("Failed to save archive", ex);
      StatusMessage = "Failed to save archive";
    }
    finally
    {
      IsBusy = false;
    }
  }

  private async Task SaveArchiveAsAsync()
  {
    if (_currentArchive == null) return;

    try
    {
      var defaultFileName = !string.IsNullOrEmpty(_currentFilePath)
        ? Path.GetFileName(_currentFilePath)
        : "archive.WD";

      var filePath = await _dialogService.ShowSaveFileDialogAsync(defaultFileName);
      if (string.IsNullOrEmpty(filePath))
        return;

      IsBusy = true;
      StatusMessage = "Saving archive...";

      await Task.Run(() => _archiver.SaveArchive(_currentArchive, filePath));

      _currentFilePath = filePath;
      HasUnsavedChanges = false;
      ArchiveInfo.FilePath = filePath;

      _notificationService.ShowSuccess($"Archive saved: {Path.GetFileName(filePath)}");
      StatusMessage = "Archive saved";
      _logger.LogInformation("Saved archive as: {FilePath}", filePath);
    }
    catch (Exception ex)
    {
      _notificationService.ShowError("Failed to save archive", ex);
      StatusMessage = "Failed to save archive";
    }
    finally
    {
      IsBusy = false;
      this.RaisePropertyChanged(nameof(WindowTitle));
    }
  }

  private async Task CloseArchiveAsync()
  {
    try
    {
      // Check for unsaved changes
      if (!await PromptSaveChangesAsync())
        return;

      _currentArchive?.Dispose();
      _currentArchive = null;
      _currentFilePath = null;

      ArchiveItems.Clear();
      SelectedItem = null;
      ArchiveInfo.Clear();
      HasUnsavedChanges = false;

      StatusMessage = "Archive closed";
      _logger.LogInformation("Closed archive");
      this.RaisePropertyChanged(nameof(WindowTitle));
    }
    catch (Exception ex)
    {
      _notificationService.ShowError("Failed to close archive", ex);
    }
    finally
    {
      this.RaisePropertyChanged(nameof(IsArchiveOpen));
    }
  }

  private async Task ExtractSelectedAsync()
  {
    if (_currentArchive == null || SelectedItem == null)
      return;

    try
    {
      var outputPath = await _dialogService.ShowFolderBrowserDialogAsync();
      if (string.IsNullOrEmpty(outputPath))
        return;

      IsBusy = true;
      StatusMessage = "Extracting file...";

      var item = SelectedItem;
      await Task.Run(() => _archiver.Extract(item.Item, outputPath));

      _notificationService.ShowSuccess($"Extracted {item.FileName} to {outputPath}");
      StatusMessage = "File extracted";
      _logger.LogInformation("Extracted file {FileName} to {OutputPath}", item.FileName, outputPath);
    }
    catch (Exception ex)
    {
      _notificationService.ShowError("Failed to extract file", ex);
      StatusMessage = "Extraction failed";
    }
    finally
    {
      IsBusy = false;
    }
  }

  private async Task ExtractAllAsync()
  {
    if (_currentArchive == null)
      return;

    try
    {
      var outputPath = await _dialogService.ShowFolderBrowserDialogAsync();
      if (string.IsNullOrEmpty(outputPath))
        return;

      IsBusy = true;
      StatusMessage = $"Extracting all files...";

      await Task.Run(() => _archiver.ExtractAll(_currentArchive, outputPath));

      _notificationService.ShowSuccess($"Extracted all files to {outputPath}");
      StatusMessage = $"Extracted {_currentArchive.Items.Count} file(s)";
      _logger.LogInformation("Extracted all files to {OutputPath}", outputPath);
    }
    catch (Exception ex)
    {
      _notificationService.ShowError("Failed to extract all files", ex);
      StatusMessage = "Extraction failed";
    }
    finally
    {
      IsBusy = false;
    }
  }

  private async Task AddFilesAsync()
  {
    if (_currentArchive == null)
      return;

    try
    {
      var files = await _dialogService.ShowOpenFilesDialogAsync();
      if (files.Count == 0)
        return;

      IsBusy = true;
      StatusMessage = $"Adding {files.Count} file(s)...";

      // Determine base directory (use parent of first file)
      var baseDirectory = Path.GetDirectoryName(files[0]);

      await Task.Run(() =>
      {
        foreach (var file in files)
        {
          _archiver.AddFile(_currentArchive, file, baseDirectory, compress: true);
        }
      });

      LoadArchiveItems();
      HasUnsavedChanges = true;

      _notificationService.ShowSuccess($"Added {files.Count} file(s) to archive");
      StatusMessage = $"Added {files.Count} file(s)";
      _logger.LogInformation("Added {Count} files to archive", files.Count);
    }
    catch (Exception ex)
    {
      _notificationService.ShowError("Failed to add files", ex);
      StatusMessage = "Failed to add files";
    }
    finally
    {
      IsBusy = false;
    }
  }

  private async Task RemoveSelectedAsync()
  {
    if (_currentArchive == null || SelectedItem == null)
      return;

    try
    {
      // Confirm deletion
      var result = await _dialogService.ShowMessageBoxAsync(
        $"Are you sure you want to remove '{SelectedItem.FileName}' from the archive?",
        "Confirm Removal",
        MessageBoxType.YesNo);

      if (result != MessageBoxResult.Yes)
        return;

      IsBusy = true;
      StatusMessage = "Removing file...";

      var item = SelectedItem.Item;
      var fileName = SelectedItem.FileName;

      await Task.Run(() => _currentArchive.RemoveItem(item));

      LoadArchiveItems();
      HasUnsavedChanges = true;

      _notificationService.ShowSuccess($"Removed {fileName} from archive");
      StatusMessage = "File removed";
      _logger.LogInformation("Removed file {FileName} from archive", fileName);
    }
    catch (Exception ex)
    {
      _notificationService.ShowError("Failed to remove file", ex);
      StatusMessage = "Failed to remove file";
    }
    finally
    {
      IsBusy = false;
    }
  }

  private void Exit()
  {
    // In a real application, this would trigger window close
    // which would then call CloseArchiveAsync
    _logger.LogInformation("Exit requested");
  }

  #endregion

  #region Helper Methods

  private void LoadArchiveItems()
  {
    ArchiveItems.Clear();
    SelectedItem = null;

    if (_currentArchive != null)
    {
      foreach (var item in _currentArchive.Items)
      {
        ArchiveItems.Add(new ArchiveItemViewModel(item));
      }

      ArchiveInfo.UpdateFromArchive(_currentArchive, _currentFilePath);
    }
    else
    {
      ArchiveInfo.Clear();
    }
  }

  private async Task<bool> PromptSaveChangesAsync()
  {
    if (!HasUnsavedChanges)
      return true;

    var result = await _dialogService.ShowMessageBoxAsync(
      "The current archive has unsaved changes. Do you want to save them?",
      "Unsaved Changes",
      MessageBoxType.YesNoCancel);

    switch (result)
    {
      case MessageBoxResult.Yes:
        await SaveArchiveAsync();
        return !HasUnsavedChanges; // Only proceed if save was successful
      case MessageBoxResult.No:
        return true;
      case MessageBoxResult.Cancel:
      default:
        return false;
    }
  }

  private void OnNotificationRaised(object? sender, NotificationEventArgs e)
  {
    // Update status message based on notification type
    StatusMessage = e.Type switch
    {
      NotificationType.Error   => $"Error: {e.Message}",
      NotificationType.Warning => $"Warning: {e.Message}",
      NotificationType.Success => e.Message,
      NotificationType.Info    => e.Message,
      _                        => e.Message
    };
  }

  #endregion

  #region IDisposable

  public void Dispose()
  {
    _currentArchive?.Dispose();
    _notificationService.NotificationRaised -= OnNotificationRaised;
  }

  #endregion
}