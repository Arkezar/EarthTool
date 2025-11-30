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
using System.Threading.Tasks;

namespace EarthTool.WD.GUI.ViewModels;

/// <summary>
/// Main ViewModel for the WD Archive Manager application.
/// </summary>
public class MainWindowViewModel : ViewModelBase, IDisposable
{
  private readonly IArchiver _archiver;
  private readonly IDialogService _dialogService;
  private readonly INotificationService _notificationService;
  private readonly ITextFlagService _textFlagService;
  private readonly ILogger<MainWindowViewModel> _logger;

  private IArchive? _currentArchive;
  private string? _currentFilePath;
  private bool _hasUnsavedChanges;
  private bool _isBusy;
  private string _statusMessage = "Ready";

  public MainWindowViewModel(
    IArchiver archiver,
    IDialogService dialogService,
    INotificationService notificationService,
    ITextFlagService textFlagService,
    ILogger<MainWindowViewModel> logger)
  {
    _archiver = archiver ?? throw new ArgumentNullException(nameof(archiver));
    _dialogService = dialogService ?? throw new ArgumentNullException(nameof(dialogService));
    _notificationService = notificationService ?? throw new ArgumentNullException(nameof(notificationService));
    _textFlagService = textFlagService ?? throw new ArgumentNullException(nameof(textFlagService));
    _logger = logger ?? throw new ArgumentNullException(nameof(logger));

    ArchiveItems = new ObservableCollection<ArchiveItemViewModel>();
    TreeItems = new ObservableCollection<TreeItemViewModel>();
    ArchiveInfo = new ArchiveInfoViewModel();

    // Subscribe to notification service
    _notificationService.NotificationRaised += OnNotificationRaised;

    InitializeCommands();
  }

  #region Properties

  public ObservableCollection<ArchiveItemViewModel> ArchiveItems { get; }
  public ObservableCollection<TreeItemViewModel> TreeItems { get; }
  public ArchiveInfoViewModel ArchiveInfo { get; }

  // For single selection scenarios
  private object? _selectedItem;

  public object? SelectedItem
  {
    get => _selectedItem;
    set
    {
      this.RaiseAndSetIfChanged(ref _selectedItem, value);
      this.RaisePropertyChanged(nameof(HasSelection));
      this.RaisePropertyChanged(nameof(SelectedTreeItem));
    }
  }

  public TreeItemViewModel? SelectedTreeItem => _selectedItem as TreeItemViewModel;

  // Track whether we have a selection (for command CanExecute)
  public bool HasSelection => SelectedTreeItem?.Item != null;

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

  public string ArchiveInfoText => IsArchiveOpen ? $"{ArchiveInfo.FilePath} | GUID: {ArchiveInfo.FormattedArchiveGuid} | Modified: {ArchiveInfo.LastModification:u} | Items: {ArchiveInfo.FormattedItemCount} | Total Size: {ArchiveInfo.FormattedTotalSize}" : "";

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
  public ReactiveCommand<Unit, Unit> AddFolderCommand { get; private set; } = null!;
  public ReactiveCommand<Unit, Unit> CreateFolderCommand { get; private set; } = null!;
  public ReactiveCommand<Unit, Unit> RemoveSelectedCommand { get; private set; } = null!;
  public ReactiveCommand<Unit, Unit> SetTextFlagCommand { get; private set; } = null!;
  public ReactiveCommand<Unit, Unit> ClearTextFlagCommand { get; private set; } = null!;
  public ReactiveCommand<Unit, Unit> ToggleTextFlagCommand { get; private set; } = null!;
  public ReactiveCommand<Unit, Unit> ToggleThemeCommand { get; private set; } = null!;
  public ReactiveCommand<Unit, Unit> AboutCommand { get; private set; } = null!;
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

    // AddFolderCommand - enabled when archive is open
    AddFolderCommand = ReactiveCommand.CreateFromTask(AddFolderAsync, canSaveAs);

    // CreateFolderCommand - enabled when archive is open
    CreateFolderCommand = ReactiveCommand.CreateFromTask(CreateFolderAsync, canSaveAs);

    // RemoveSelectedCommand - enabled when items are selected
    RemoveSelectedCommand = ReactiveCommand.CreateFromTask(RemoveSelectedAsync, canExtractSelected);

    // Text flag commands - enabled when items are selected
    SetTextFlagCommand = ReactiveCommand.CreateFromTask(SetTextFlagAsync, canExtractSelected);
    ClearTextFlagCommand = ReactiveCommand.CreateFromTask(ClearTextFlagAsync, canExtractSelected);
    ToggleTextFlagCommand = ReactiveCommand.CreateFromTask(ToggleTextFlagAsync, canExtractSelected);

    // ToggleThemeCommand - always enabled
    ToggleThemeCommand = ReactiveCommand.Create(ToggleTheme);

    // AboutCommand - always enabled
    AboutCommand = ReactiveCommand.CreateFromTask(ShowAboutAsync);

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

      // Validate file path is writable before attempting save
      var directory = Path.GetDirectoryName(filePath);
      if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
      {
        _notificationService.ShowError($"Directory does not exist: {directory}");
        StatusMessage = "Failed to save archive";
        return;
      }

      // Save archive with closeArchiveBeforeSave=true to release file locks
      // This is necessary on Windows 11 when saving to the same file
      await Task.Run(() => _archiver.SaveArchive(_currentArchive, filePath, closeArchiveBeforeSave: true));

      // After save, the archive has been disposed (to release file locks)
      // We need to reopen it to continue working
      _logger.LogInformation("Reopening archive after save");
      IArchive? reopenedArchive = null;
      await Task.Run(() => reopenedArchive = _archiver.OpenArchive(filePath));

      _currentArchive = reopenedArchive;

      // Refresh UI to reflect the reopened archive, preserving selection and expansion state
      LoadArchiveItemsPreservingState();

      HasUnsavedChanges = false;
      _notificationService.ShowSuccess($"Archive saved: {Path.GetFileName(filePath)}");
      StatusMessage = "Archive saved";
      _logger.LogInformation("Saved archive: {FilePath}", filePath);
    }
    catch (UnauthorizedAccessException ex)
    {
      _logger.LogError(ex, "Access denied when saving archive to {FilePath}", _currentFilePath);
      _notificationService.ShowError($"Access denied. Cannot save to: {_currentFilePath}\nTry saving to a different location or run as administrator.", ex);
      StatusMessage = "Failed to save archive - Access denied";
    }
    catch (IOException ex)
    {
      _logger.LogError(ex, "IO error when saving archive to {FilePath}", _currentFilePath);
      _notificationService.ShowError($"Cannot save archive. The file may be in use by another program.\n\n{ex.Message}", ex);
      StatusMessage = "Failed to save archive - File in use";
    }
    catch (Exception ex)
    {
      _logger.LogError(ex, "Unexpected error when saving archive to {FilePath}", _currentFilePath);
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
        : "archive.wd";

      var filePath = await _dialogService.ShowSaveFileDialogAsync(defaultFileName);
      if (string.IsNullOrEmpty(filePath))
        return;

      IsBusy = true;
      StatusMessage = "Saving archive...";

      // Validate file path is writable before attempting save
      var directory = Path.GetDirectoryName(filePath);
      if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
      {
        _notificationService.ShowError($"Directory does not exist: {directory}");
        StatusMessage = "Failed to save archive";
        return;
      }

      await Task.Run(() => _archiver.SaveArchive(_currentArchive, filePath));

      _currentFilePath = filePath;
      HasUnsavedChanges = false;
      ArchiveInfo.FilePath = filePath;

      _notificationService.ShowSuccess($"Archive saved: {Path.GetFileName(filePath)}");
      StatusMessage = "Archive saved";
      _logger.LogInformation("Saved archive as: {FilePath}", filePath);
    }
    catch (UnauthorizedAccessException ex)
    {
      _logger.LogError(ex, "Access denied when saving archive to {FilePath}", _currentFilePath);
      _notificationService.ShowError($"Access denied. Cannot save to this location.\nTry saving to a different location or run as administrator.", ex);
      StatusMessage = "Failed to save archive - Access denied";
    }
    catch (IOException ex)
    {
      _logger.LogError(ex, "IO error when saving archive");
      _notificationService.ShowError($"Cannot save archive. The file may be in use by another program.\n\n{ex.Message}", ex);
      StatusMessage = "Failed to save archive - File in use";
    }
    catch (Exception ex)
    {
      _logger.LogError(ex, "Unexpected error when saving archive");
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
      TreeItems.Clear();
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
    if (_currentArchive == null || SelectedTreeItem?.Item == null)
      return;

    try
    {
      var outputPath = await _dialogService.ShowFolderBrowserDialogAsync();
      if (string.IsNullOrEmpty(outputPath))
        return;

      IsBusy = true;
      StatusMessage = "Extracting file...";

      var item = SelectedTreeItem;
      await Task.Run(() => _archiver.Extract(item.Item!, outputPath));

      _notificationService.ShowSuccess($"Extracted {item.Name} to {outputPath}");
      StatusMessage = "File extracted";
      _logger.LogInformation("Extracted file {FileName} to {OutputPath}", item.Name, outputPath);
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

      // Determine target folder in archive
      var targetFolder = "";
      if (SelectedTreeItem != null)
      {
        // If folder is selected, use it as target
        // If file is selected, use its parent folder
        targetFolder = SelectedTreeItem.IsFolder
          ? SelectedTreeItem.FullPath
          : Path.GetDirectoryName(SelectedTreeItem.FullPath)?.Replace("\\", "/") ?? "";
      }

      var targetMessage = string.IsNullOrEmpty(targetFolder)
        ? "to archive root"
        : $"to folder '{targetFolder}'";

      StatusMessage = $"Adding {files.Count} file(s) {targetMessage}...";

      // To add files to specific folder in archive, we need to create a temporary directory structure
      // that mirrors the desired archive structure
      string tempBaseDir;

      if (!string.IsNullOrEmpty(targetFolder))
      {
        // Create temp directory with target folder structure
        tempBaseDir = Path.Combine(Path.GetTempPath(), $"EarthToolTemp_{Guid.NewGuid()}");
        var targetPath = Path.Combine(tempBaseDir, targetFolder.Replace("/", Path.DirectorySeparatorChar.ToString()));
        Directory.CreateDirectory(targetPath);

        // Copy files to temp location
        var tempFiles = new List<string>();
        foreach (var file in files)
        {
          var fileName = Path.GetFileName(file);
          var tempFile = Path.Combine(targetPath, fileName);
          File.Copy(file, tempFile);
          tempFiles.Add(tempFile);
        }

        // Add files from temp location with proper base directory
        await Task.Run(() =>
        {
          foreach (var tempFile in tempFiles)
          {
            _archiver.AddFile(_currentArchive, tempFile, tempBaseDir, compress: true);
          }
        });

        // Cleanup temp directory
        try
        {
          Directory.Delete(tempBaseDir, true);
        }
        catch (Exception ex)
        {
          _logger.LogWarning(ex, "Failed to cleanup temp directory {TempDir}", tempBaseDir);
        }
      }
      else
      {
        // Add to root - use original method
        var baseDirectory = Path.GetDirectoryName(files[0]);
        await Task.Run(() =>
        {
          foreach (var file in files)
          {
            _archiver.AddFile(_currentArchive, file, baseDirectory, compress: true);
          }
        });
      }

      LoadArchiveItemsPreservingState();
      HasUnsavedChanges = true;

      _notificationService.ShowSuccess($"Added {files.Count} file(s) {targetMessage}");
      StatusMessage = $"Added {files.Count} file(s)";
      _logger.LogInformation("Added {Count} files to archive at path '{TargetFolder}'", files.Count, targetFolder);
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

  private async Task AddFolderAsync()
  {
    if (_currentArchive == null)
      return;

    try
    {
      var folderPath = await _dialogService.ShowFolderBrowserDialogAsync();
      if (string.IsNullOrEmpty(folderPath))
        return;

      IsBusy = true;

      // Determine target folder in archive
      var targetFolder = "";
      if (SelectedTreeItem != null)
      {
        // If folder is selected, use it as target
        // If file is selected, use its parent folder
        targetFolder = SelectedTreeItem.IsFolder
          ? SelectedTreeItem.FullPath
          : Path.GetDirectoryName(SelectedTreeItem.FullPath)?.Replace("\\", "/") ?? "";
      }

      var folderName = Path.GetFileName(folderPath);
      var targetMessage = string.IsNullOrEmpty(targetFolder)
        ? $"to archive root"
        : $"to folder '{targetFolder}'";

      StatusMessage = $"Adding folder '{folderName}' {targetMessage}...";

      // Get all files in the folder recursively
      var allFiles = Directory.GetFiles(folderPath, "*", SearchOption.AllDirectories);

      if (allFiles.Length == 0)
      {
        _notificationService.ShowWarning($"Folder '{folderName}' is empty - no files to add");
        return;
      }

      // To add folder to specific location in archive, we need to create a temporary directory structure
      string tempBaseDir;

      if (!string.IsNullOrEmpty(targetFolder))
      {
        // Create temp directory with target folder structure
        tempBaseDir = Path.Combine(Path.GetTempPath(), $"EarthToolTemp_{Guid.NewGuid()}");
        var targetPath = Path.Combine(tempBaseDir, targetFolder.Replace("/", Path.DirectorySeparatorChar.ToString()));
        Directory.CreateDirectory(targetPath);

        // Copy entire folder structure to temp location
        var destFolderPath = Path.Combine(targetPath, folderName);
        CopyDirectory(folderPath, destFolderPath);

        // Add all files from temp location with proper base directory
        var tempFiles = Directory.GetFiles(destFolderPath, "*", SearchOption.AllDirectories);
        await Task.Run(() =>
        {
          foreach (var tempFile in tempFiles)
          {
            _archiver.AddFile(_currentArchive, tempFile, tempBaseDir, compress: true);
          }
        });

        // Cleanup temp directory
        try
        {
          Directory.Delete(tempBaseDir, true);
        }
        catch (Exception ex)
        {
          _logger.LogWarning(ex, "Failed to cleanup temp directory {TempDir}", tempBaseDir);
        }
      }
      else
      {
        // Add to root - use the parent directory as base
        var baseDirectory = Path.GetDirectoryName(folderPath);
        await Task.Run(() =>
        {
          foreach (var file in allFiles)
          {
            _archiver.AddFile(_currentArchive, file, baseDirectory, compress: true);
          }
        });
      }

      LoadArchiveItemsPreservingState();
      HasUnsavedChanges = true;

      _notificationService.ShowSuccess($"Added folder '{folderName}' with {allFiles.Length} file(s) {targetMessage}");
      StatusMessage = $"Added {allFiles.Length} file(s)";
      _logger.LogInformation("Added folder '{FolderName}' with {Count} files to archive at path '{TargetFolder}'",
        folderName, allFiles.Length, targetFolder);
    }
    catch (Exception ex)
    {
      _notificationService.ShowError("Failed to add folder", ex);
      StatusMessage = "Failed to add folder";
    }
    finally
    {
      IsBusy = false;
    }
  }

  private async Task CreateFolderAsync()
  {
    if (_currentArchive == null)
      return;

    try
    {
      // Ask user for folder name
      var folderName = await _dialogService.ShowInputDialogAsync(
        "Enter the name for the new folder:",
        "Create Folder",
        "NewFolder");

      if (string.IsNullOrWhiteSpace(folderName))
        return;

      // Sanitize folder name (remove invalid characters)
      var invalidChars = Path.GetInvalidFileNameChars();
      folderName = string.Join("_", folderName.Split(invalidChars, StringSplitOptions.RemoveEmptyEntries));

      if (string.IsNullOrWhiteSpace(folderName))
      {
        _notificationService.ShowWarning("Invalid folder name");
        return;
      }

      // Determine target folder in archive
      var targetFolder = "";
      if (SelectedTreeItem != null)
      {
        // If folder is selected, create inside it
        // If file is selected, create in its parent folder
        targetFolder = SelectedTreeItem.IsFolder
          ? SelectedTreeItem.FullPath
          : Path.GetDirectoryName(SelectedTreeItem.FullPath)?.Replace("\\", "/") ?? "";
      }

      var fullPath = string.IsNullOrEmpty(targetFolder)
        ? folderName
        : $"{targetFolder}/{folderName}";

      // Check if folder already exists
      if (FolderExistsInTree(fullPath))
      {
        _notificationService.ShowWarning($"Folder '{folderName}' already exists in this location");
        return;
      }

      // Create virtual folder by adding it to the tree
      // We need to add a placeholder file to actually create the folder in the archive
      // since WD archives don't support empty folders
      var placeholderFileName = $"{fullPath}/.placeholder";

      // Create a temporary placeholder file
      var tempFile = Path.Combine(Path.GetTempPath(), $"placeholder_{Guid.NewGuid()}.tmp");
      File.WriteAllText(tempFile, "This is a placeholder file to maintain folder structure.");

      try
      {
        // Create temp directory structure
        var tempBaseDir = Path.Combine(Path.GetTempPath(), $"EarthToolTemp_{Guid.NewGuid()}");
        var placeholderPath = Path.Combine(tempBaseDir, placeholderFileName.Replace("/", Path.DirectorySeparatorChar.ToString()));
        var placeholderDir = Path.GetDirectoryName(placeholderPath);

        if (!string.IsNullOrEmpty(placeholderDir))
        {
          Directory.CreateDirectory(placeholderDir);
          File.Copy(tempFile, placeholderPath);

          await Task.Run(() =>
          {
            _archiver.AddFile(_currentArchive, placeholderPath, tempBaseDir, compress: true);
          });

          // Cleanup
          try
          {
            Directory.Delete(tempBaseDir, true);
          }
          catch (Exception ex)
          {
            _logger.LogWarning(ex, "Failed to cleanup temp directory {TempDir}", tempBaseDir);
          }
        }
      }
      finally
      {
        // Cleanup temp file
        try
        {
          File.Delete(tempFile);
        }
        catch (Exception ex)
        {
          _logger.LogWarning(ex, "Failed to delete temp file {TempFile}", tempFile);
        }
      }

      LoadArchiveItemsPreservingState();
      HasUnsavedChanges = true;

      _notificationService.ShowSuccess($"Created folder '{folderName}' at {(string.IsNullOrEmpty(targetFolder) ? "root" : targetFolder)}");
      StatusMessage = $"Created folder '{folderName}'";
      _logger.LogInformation("Created folder '{FolderName}' at path '{TargetFolder}'", folderName, targetFolder);
    }
    catch (Exception ex)
    {
      _notificationService.ShowError("Failed to create folder", ex);
      StatusMessage = "Failed to create folder";
    }
  }

  private bool FolderExistsInTree(string fullPath)
  {
    return TreeItems.Any(item => CheckFolderExists(item, fullPath));
  }

  private bool CheckFolderExists(TreeItemViewModel item, string fullPath)
  {
    if (item.IsFolder && item.FullPath.Equals(fullPath, StringComparison.OrdinalIgnoreCase))
      return true;

    return item.Children.Any(child => CheckFolderExists(child, fullPath));
  }

  private void CopyDirectory(string sourceDir, string destDir)
  {
    Directory.CreateDirectory(destDir);

    // Copy all files
    foreach (var file in Directory.GetFiles(sourceDir))
    {
      var fileName = Path.GetFileName(file);
      var destFile = Path.Combine(destDir, fileName);
      File.Copy(file, destFile);
    }

    // Recursively copy subdirectories
    foreach (var directory in Directory.GetDirectories(sourceDir))
    {
      var dirName = Path.GetFileName(directory);
      var destSubDir = Path.Combine(destDir, dirName);
      CopyDirectory(directory, destSubDir);
    }
  }

  private async Task RemoveSelectedAsync()
  {
    if (_currentArchive == null || SelectedTreeItem?.Item == null)
      return;

    try
    {
      // Confirm deletion
      var result = await _dialogService.ShowMessageBoxAsync(
        $"Are you sure you want to remove '{SelectedTreeItem.Name}' from the archive?",
        "Confirm Removal",
        MessageBoxType.YesNo);

      if (result != MessageBoxResult.Yes)
        return;

      IsBusy = true;
      StatusMessage = "Removing file...";

      var item = SelectedTreeItem.Item;
      var fileName = SelectedTreeItem.Name;
      var itemPath = SelectedTreeItem.FullPath;

      await Task.Run(() => _currentArchive.RemoveItem(item!));

      // Use preserving state method to maintain tree expansion and try to select parent
      LoadArchiveItemsPreservingState(GetParentPath(itemPath));

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

  private Task SetTextFlagAsync()
  {
    if (_currentArchive == null || SelectedTreeItem?.Item == null)
      return Task.CompletedTask;

    try
    {
      var item = SelectedTreeItem;
      _textFlagService.SetTextFlag(item.Item!);

      // Refresh TreeView while preserving expansion state and selection
      LoadArchiveItemsPreservingState(item.FullPath);

      HasUnsavedChanges = true;
      _notificationService.ShowSuccess($"Text flag set for '{item.Name}'");
      _logger.LogInformation("Text flag set for file {FileName}", item.Name);
    }
    catch (Exception ex)
    {
      _notificationService.ShowError("Failed to set Text flag", ex);
      _logger.LogError(ex, "Failed to set Text flag");
    }

    return Task.CompletedTask;
  }

  private Task ClearTextFlagAsync()
  {
    if (_currentArchive == null || SelectedTreeItem?.Item == null)
      return Task.CompletedTask;

    try
    {
      var item = SelectedTreeItem;
      _textFlagService.ClearTextFlag(item.Item!);

      // Refresh TreeView while preserving expansion state and selection
      LoadArchiveItemsPreservingState(item.FullPath);

      HasUnsavedChanges = true;
      _notificationService.ShowSuccess($"Text flag cleared for '{item.Name}'");
      _logger.LogInformation("Text flag cleared for file {FileName}", item.Name);
    }
    catch (Exception ex)
    {
      _notificationService.ShowError("Failed to clear Text flag", ex);
      _logger.LogError(ex, "Failed to clear Text flag");
    }

    return Task.CompletedTask;
  }

  private Task ToggleTextFlagAsync()
  {
    if (_currentArchive == null || SelectedTreeItem?.Item == null)
      return Task.CompletedTask;

    var item = SelectedTreeItem;
    if (_textFlagService.HasTextFlag(item.Item!))
    {
      // Clear text flag while preserving tree state
      _textFlagService.ClearTextFlag(item.Item!);
      LoadArchiveItemsPreservingState(item.FullPath);
      HasUnsavedChanges = true;
      _notificationService.ShowSuccess($"Text flag cleared for '{item.Name}'");
      _logger.LogInformation("Text flag cleared for file {FileName}", item.Name);
    }
    else
    {
      // Set text flag while preserving tree state
      _textFlagService.SetTextFlag(item.Item!);
      LoadArchiveItemsPreservingState(item.FullPath);
      HasUnsavedChanges = true;
      _notificationService.ShowSuccess($"Text flag set for '{item.Name}'");
      _logger.LogInformation("Text flag set for file {FileName}", item.Name);
    }

    return Task.CompletedTask;
  }

  private void ToggleTheme()
  {
    try
    {
      var app = Avalonia.Application.Current;
      if (app != null)
      {
        var currentTheme = app.ActualThemeVariant;
        app.RequestedThemeVariant = currentTheme == Avalonia.Styling.ThemeVariant.Dark
          ? Avalonia.Styling.ThemeVariant.Light
          : Avalonia.Styling.ThemeVariant.Dark;

        _logger.LogInformation("Theme toggled to: {Theme}", app.RequestedThemeVariant);
      }
    }
    catch (Exception ex)
    {
      _logger.LogError(ex, "Failed to toggle theme");
      _notificationService.ShowError("Failed to toggle theme", ex);
    }
  }

  private async Task ShowAboutAsync()
  {
    try
    {
      var aboutViewModel = new AboutViewModel();
      var aboutView = new Views.AboutView
      {
        DataContext = aboutViewModel
      };

      await _dialogService.ShowCustomDialogAsync(
        aboutView,
        "About EarthTool",
        width: 550,
        height: 550);

      _logger.LogInformation("About dialog shown");
    }
    catch (Exception ex)
    {
      _logger.LogError(ex, "Failed to show About dialog");
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
    TreeItems.Clear();
    SelectedItem = null;

    if (_currentArchive != null)
    {
      foreach (var item in _currentArchive.Items)
      {
        ArchiveItems.Add(new ArchiveItemViewModel(item));
      }

      BuildTreeStructure();
      ArchiveInfo.UpdateFromArchive(_currentArchive, _currentFilePath);
      this.RaisePropertyChanged(nameof(ArchiveInfoText));
    }
    else
    {
      ArchiveInfo.Clear();
      this.RaisePropertyChanged(nameof(ArchiveInfoText));
    }
  }

  /// <summary>
  /// Loads archive items while preserving tree expansion state and selection.
  /// </summary>
  /// <param name="preserveSelectionPath">The path of the item to try to preserve selection for</param>
  private void LoadArchiveItemsPreservingState(string? preserveSelectionPath = null)
  {
    // Store current expansion state
    var expandedPaths = GetExpandedPaths();

    // Store current selection path
    var selectedPath = SelectedTreeItem?.FullPath ?? preserveSelectionPath;

    // Clear and rebuild
    ArchiveItems.Clear();
    TreeItems.Clear();
    SelectedItem = null;

    if (_currentArchive != null)
    {
      foreach (var item in _currentArchive.Items)
      {
        ArchiveItems.Add(new ArchiveItemViewModel(item));
      }

      BuildTreeStructure();

      // Restore expansion state
      RestoreExpandedPaths(expandedPaths);

      // Try to restore selection
      if (!string.IsNullOrEmpty(selectedPath))
      {
        RestoreSelectionByPath(selectedPath);
      }

      ArchiveInfo.UpdateFromArchive(_currentArchive, _currentFilePath);
      this.RaisePropertyChanged(nameof(ArchiveInfoText));
    }
    else
    {
      ArchiveInfo.Clear();
      this.RaisePropertyChanged(nameof(ArchiveInfoText));
    }
  }

  /// <summary>
  /// Gets the paths of all expanded items in the tree.
  /// </summary>
  private HashSet<string> GetExpandedPaths()
  {
    var expandedPaths = new HashSet<string>();

    foreach (var rootItem in TreeItems)
    {
      CollectExpandedPaths(rootItem, expandedPaths);
    }

    System.Diagnostics.Debug.WriteLine($"Collected {expandedPaths.Count} expanded paths: {string.Join(", ", expandedPaths)}");
    return expandedPaths;
  }

  /// <summary>
  /// Recursively collects expanded paths from the tree.
  /// </summary>
  private void CollectExpandedPaths(TreeItemViewModel item, HashSet<string> expandedPaths)
  {
    if (item.IsExpanded && !string.IsNullOrEmpty(item.FullPath))
    {
      expandedPaths.Add(item.FullPath);
    }

    foreach (var child in item.Children)
    {
      CollectExpandedPaths(child, expandedPaths);
    }
  }

  /// <summary>
  /// Restores the expansion state of items in the tree.
  /// </summary>
  private void RestoreExpandedPaths(HashSet<string> expandedPaths)
  {
    foreach (var rootItem in TreeItems)
    {
      RestoreExpandedPathsRecursive(rootItem, expandedPaths);
    }
  }

  /// <summary>
  /// Recursively restores expansion state in the tree.
  /// </summary>
  private void RestoreExpandedPathsRecursive(TreeItemViewModel item, HashSet<string> expandedPaths)
  {
    if (expandedPaths.Contains(item.FullPath))
    {
      item.IsExpanded = true;
    }

    foreach (var child in item.Children)
    {
      RestoreExpandedPathsRecursive(child, expandedPaths);
    }
  }

  /// <summary>
  /// Tries to restore selection by finding an item with the specified path.
  /// </summary>
  private void RestoreSelectionByPath(string path)
  {
    // Try to find the exact item first
    var foundItem = FindItemByPath(TreeItems, path);
    if (foundItem != null)
    {
      SelectedItem = foundItem;
      return;
    }

    // If not found, try to select the parent folder
    var parentPath = GetParentPath(path);
    if (!string.IsNullOrEmpty(parentPath))
    {
      var parentItem = FindItemByPath(TreeItems, parentPath);
      if (parentItem != null)
      {
        SelectedItem = parentItem;
        // Expand the parent to show its children
        parentItem.IsExpanded = true;
      }
    }
  }

  /// <summary>
  /// Finds an item in the tree by its full path.
  /// </summary>
  private TreeItemViewModel? FindItemByPath(ObservableCollection<TreeItemViewModel> items, string path)
  {
    foreach (var item in items)
    {
      if (item.FullPath.Equals(path, StringComparison.OrdinalIgnoreCase))
      {
        return item;
      }

      var foundInChildren = FindItemByPath(item.Children, path);
      if (foundInChildren != null)
      {
        return foundInChildren;
      }
    }
    return null;
  }

  /// <summary>
  /// Gets the parent path of a given path.
  /// </summary>
  private string? GetParentPath(string path)
  {
    var lastSlash = path.LastIndexOf('/');
    if (lastSlash > 0)
    {
      return path.Substring(0, lastSlash);
    }
    else if (lastSlash == 0)
    {
      return "/"; // Root
    }
    return null;
  }

  private void BuildTreeStructure()
  {
    // Clear existing tree items to prevent duplication
    TreeItems.Clear();

    var root = new Dictionary<string, TreeItemViewModel>();

    foreach (var archiveItem in ArchiveItems)
    {
      var fileName = archiveItem.FileName;
      var parts = fileName.Split(new[] { '/', '\\' }, StringSplitOptions.RemoveEmptyEntries);

      TreeItemViewModel? currentParent = null;
      string currentPath = "";

      for (int i = 0; i < parts.Length; i++)
      {
        var part = parts[i];
        var isFile = i == parts.Length - 1;
        currentPath = string.IsNullOrEmpty(currentPath) ? part : $"{currentPath}/{part}";

        TreeItemViewModel node;

        if (currentParent == null)
        {
          // Root level
          if (!root.ContainsKey(part))
          {
            node = new TreeItemViewModel(part, !isFile)
            {
              FullPath = currentPath,
              Item = isFile ? archiveItem.Item : null,
            };
            root[part] = node;
            TreeItems.Add(node);
          }
          else
          {
            node = root[part];
          }
        }
        else
        {
          // Child level
          var existingChild = currentParent.Children.FirstOrDefault(c => c.Name == part);
          if (existingChild == null)
          {
            node = new TreeItemViewModel(part, !isFile)
            {
              FullPath = currentPath,
              Item = isFile ? archiveItem.Item : null,
            };
            currentParent.Children.Add(node);
          }
          else
          {
            node = existingChild;
          }
        }

        currentParent = node;
      }
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
      NotificationType.Error => $"Error: {e.Message}",
      NotificationType.Warning => $"Warning: {e.Message}",
      NotificationType.Success => e.Message,
      NotificationType.Info => e.Message,
      _ => e.Message
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
