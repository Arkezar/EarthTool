using Avalonia.Media.Imaging;
using EarthTool.Common.GUI.Interfaces;
using EarthTool.Common.GUI.ViewModels;
using EarthTool.Common.Interfaces;
using EarthTool.TEX.Interfaces;
using Microsoft.Extensions.Logging;
using ReactiveUI;
using SkiaSharp;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Reactive;
using System.Threading.Tasks;

namespace EarthTool.TEX.GUI.ViewModels;

public class MainWindowViewModel : ViewModelBase
{
  private readonly IReader<ITexFile>            _reader;
  private readonly IDialogService               _dialogService;
  private readonly INotificationService         _notificationService;
  private readonly ILogger<MainWindowViewModel> _logger;
  private          string?                      _currentFilePath;
  private          string?                      _currentFolderPath;
  private          ITexFile?                    _currentTexFile;
  private          ObservableCollection<Bitmap> _images = new();
  private          int                          _selectedImageIndex;
  private          Bitmap?                      _selectedImage;
  private          ObservableCollection<string> _texFiles         = new();
  private          int                          _currentFileIndex = -1;

  /// <summary>
  /// Gets the current file path.
  /// </summary>
  public string? CurrentFilePath => _currentFilePath;

  /// <summary>
  /// Gets the current folder path.
  /// </summary>
  public string? CurrentFolderPath => _currentFolderPath;

  /// <summary>
  /// Gets the collection of loaded images.
  /// </summary>
  public ObservableCollection<Bitmap> Images => _images;

  /// <summary>
  /// Gets the collection of TEX files in the current folder.
  /// </summary>
  public ObservableCollection<string> TexFiles => _texFiles;

  /// <summary>
  /// Gets or sets the current file index in the folder.
  /// </summary>
  public int CurrentFileIndex
  {
    get => _currentFileIndex;
    set
    {
      if (_currentFileIndex != value && value >= 0 && value < _texFiles.Count)
      {
        _currentFileIndex = value;
        this.RaisePropertyChanged();
        LoadTexFile(_texFiles[value]);
      }
    }
  }

  /// <summary>
  /// Gets the current file name.
  /// </summary>
  public string CurrentFileName
    => string.IsNullOrEmpty(_currentFilePath)
      ? "No file loaded"
      : System.IO.Path.GetFileName(_currentFilePath);

  /// <summary>
  /// Gets whether there is a previous file available.
  /// </summary>
  public bool HasPreviousFile => _currentFileIndex > 0;

  /// <summary>
  /// Gets whether there is a next file available.
  /// </summary>
  public bool HasNextFile => _currentFileIndex >= 0 && _currentFileIndex < _texFiles.Count - 1;

  /// <summary>
  /// Gets or sets the currently selected image.
  /// </summary>
  public Bitmap? SelectedImage
  {
    get => _selectedImage;
    set => this.RaiseAndSetIfChanged(ref _selectedImage, value);
  }

  /// <summary>
  /// Gets or sets the index of the selected image.
  /// </summary>
  public int SelectedImageIndex
  {
    get => _selectedImageIndex;
    set
    {
      this.RaiseAndSetIfChanged(ref _selectedImageIndex, value);
      if (value >= 0 && value < _images.Count)
      {
        SelectedImage = _images[value];
      }
    }
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
        title = $"{fileName} - {title}";
      }

      return title;
    }
  }

  public ReactiveCommand<Unit, Unit> OpenFileCommand { get; private set; } = null!;
  public ReactiveCommand<Unit, Unit> OpenFolderCommand { get; private set; } = null!;
  public ReactiveCommand<Unit, Unit> ShowAboutCommand { get; private set; } = null!;
  public ReactiveCommand<Unit, Unit> NextFileCommand { get; private set; } = null!;
  public ReactiveCommand<Unit, Unit> PreviousFileCommand { get; private set; } = null!;

  public MainWindowViewModel(
    IReader<ITexFile> reader,
    IDialogService dialogService,
    INotificationService notificationService,
    ILogger<MainWindowViewModel> logger)
  {
    _reader = reader;
    _dialogService = dialogService             ?? throw new ArgumentNullException(nameof(dialogService));
    _notificationService = notificationService ?? throw new ArgumentNullException(nameof(notificationService));
    _logger = logger                           ?? throw new ArgumentNullException(nameof(logger));
    InitializeCommands();
    _logger.LogInformation("TexViewModel initialized");
  }

  private void InitializeCommands()
  {
    OpenFileCommand = ReactiveCommand.CreateFromTask(OpenFileAsync);
    OpenFolderCommand = ReactiveCommand.CreateFromTask(OpenFolderAsync);

    var canNavigate = this.WhenAnyValue(
      x => x.CurrentFileIndex,
      x => x.TexFiles.Count,
      (index, count) => count > 0);

    var canGoPrevious = this.WhenAnyValue(
      x => x.CurrentFileIndex,
      index => index > 0);

    var canGoNext = this.WhenAnyValue(
      x => x.CurrentFileIndex,
      x => x.TexFiles.Count,
      (index, count) => index >= 0 && index < count - 1);

    PreviousFileCommand = ReactiveCommand.Create(LoadPreviousFile, canGoPrevious);
    NextFileCommand = ReactiveCommand.Create(LoadNextFile, canGoNext);

    ShowAboutCommand = ReactiveCommand.CreateFromTask(() => _dialogService.ShowAboutAsync(new TexAboutViewModel()));

    this.WhenAnyValue(x => x.CurrentFilePath)
      .Subscribe(_ =>
      {
        this.RaisePropertyChanged(nameof(WindowTitle));
        this.RaisePropertyChanged(nameof(CurrentFileName));
      });

    this.WhenAnyValue(x => x.CurrentFileIndex)
      .Subscribe(_ =>
      {
        this.RaisePropertyChanged(nameof(HasPreviousFile));
        this.RaisePropertyChanged(nameof(HasNextFile));
      });
  }

  private async Task OpenFileAsync()
  {
    try
    {
      var filePath = (await _dialogService.ShowOpenFilesDialogAsync("Open TEX File", false, ("TEX Files", "*.tex")))
        .FirstOrDefault();
      if (string.IsNullOrEmpty(filePath))
        return;

      LoadTexFile(filePath);

      // Update folder context
      var directory = System.IO.Path.GetDirectoryName(filePath);
      if (!string.IsNullOrEmpty(directory))
      {
        _currentFolderPath = directory;
        LoadFilesFromFolder(directory);
        SetCurrentFileIndex(_texFiles.IndexOf(filePath));
      }
    }
    catch (Exception ex)
    {
      _logger.LogError(ex, "Failed to open TEX file");
      _notificationService.ShowError("Failed to open file", ex);
    }
  }

  private async Task OpenFolderAsync()
  {
    try
    {
      var folderPath = await _dialogService.ShowFolderBrowserDialogAsync("Select Folder with TEX Files");
      if (string.IsNullOrEmpty(folderPath))
        return;

      _logger.LogInformation("Opening folder: {FolderPath}", folderPath);
      _currentFolderPath = folderPath;

      LoadFilesFromFolder(folderPath);

      if (_texFiles.Count > 0)
      {
        SetCurrentFileIndex(0);
        LoadTexFile(_texFiles[0]);
        _notificationService.ShowSuccess($"Found {_texFiles.Count} TEX files");
      }
      else
      {
        _notificationService.ShowWarning("No TEX files found in the selected folder");
      }
    }
    catch (Exception ex)
    {
      _logger.LogError(ex, "Failed to open folder");
      _notificationService.ShowError("Failed to open folder", ex);
    }
  }

  private void SetCurrentFileIndex(int index)
  {
    _currentFileIndex = index;
    this.RaisePropertyChanged(nameof(CurrentFileIndex));
  }

  private void LoadFilesFromFolder(string folderPath)
  {
    _texFiles.Clear();

    var files = System.IO.Directory.GetFiles(folderPath, "*.tex",
        new EnumerationOptions() { MatchCasing = MatchCasing.CaseInsensitive, RecurseSubdirectories = true })
      .OrderBy(f => f)
      .ToList();

    foreach (var file in files)
    {
      _texFiles.Add(file);
    }

    _logger.LogInformation("Found {Count} TEX files in folder", _texFiles.Count);
  }

  private void LoadTexFile(string filePath)
  {
    try
    {
      _logger.LogInformation("Opening TEX file: {FilePath}", filePath);
      _currentFilePath = filePath;

      _currentTexFile = _reader.Read(_currentFilePath);

      LoadImages();

      this.RaisePropertyChanged(nameof(CurrentFilePath));
      _notificationService.ShowSuccess($"Opened {System.IO.Path.GetFileName(filePath)}");
    }
    catch (Exception ex)
    {
      _logger.LogError(ex, "Failed to load TEX file: {FilePath}", filePath);
      _notificationService.ShowError($"Failed to load {System.IO.Path.GetFileName(filePath)}", ex);
    }
  }

  private void LoadPreviousFile()
  {
    if (_currentFileIndex > 0)
    {
      CurrentFileIndex = _currentFileIndex - 1;
    }
  }

  private void LoadNextFile()
  {
    if (_currentFileIndex < _texFiles.Count - 1)
    {
      CurrentFileIndex = _currentFileIndex + 1;
    }
  }

  private void LoadImages()
  {
    _images.Clear();

    if (_currentTexFile == null)
      return;

    foreach (var imageGroup in _currentTexFile.Images)
    {
      foreach (var texImage in imageGroup)
      {
        // Get the first mipmap (highest resolution)
        var firstMipmap = texImage.Mipmaps.FirstOrDefault();
        if (firstMipmap != null)
        {
          var avaloniaImage = ConvertToAvaloniaImage(firstMipmap);
          if (avaloniaImage != null)
          {
            _images.Add(avaloniaImage);
          }
        }
      }
    }

    if (_images.Count > 0)
    {
      SelectedImageIndex = 0;
    }

    _logger.LogInformation("Loaded {Count} images", _images.Count);
  }

  private Bitmap? ConvertToAvaloniaImage(SKBitmap skBitmap)
  {
    try
    {
      using (var image = SKImage.FromBitmap(skBitmap))
      using (var data = image.Encode(SKEncodedImageFormat.Png, 100))
      using (var stream = new MemoryStream())
      {
        data.SaveTo(stream);
        stream.Seek(0, SeekOrigin.Begin);
        return new Bitmap(stream);
      }
    }
    catch (Exception ex)
    {
      _logger.LogError(ex, "Failed to convert SKBitmap to Avalonia Bitmap");
      return null;
    }
  }
}
