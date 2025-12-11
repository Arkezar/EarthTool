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
  private          List<TexImage>               _texImages        = new();
  private          int                          _selectedImageIndex;
  private          Bitmap?                      _selectedImage;
  private          ObservableCollection<string> _texFiles         = new();
  private          int                          _currentFileIndex = -1;
  private          string                       _headerInfo       = string.Empty;
  private          string                       _selectedImageHeaderInfo = string.Empty;

  /// <summary>
  /// Gets the current file path.
  /// </summary>
  public string? CurrentFilePath => _currentFilePath;

  /// <summary>
  /// Gets the header information as formatted text.
  /// </summary>
  public string HeaderInfo
  {
    get => _headerInfo;
    private set => this.RaiseAndSetIfChanged(ref _headerInfo, value);
  }

  /// <summary>
  /// Gets the selected image header information as formatted text.
  /// </summary>
  public string SelectedImageHeaderInfo
  {
    get => _selectedImageHeaderInfo;
    private set => this.RaiseAndSetIfChanged(ref _selectedImageHeaderInfo, value);
  }

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
        UpdateSelectedImageHeaderInfo(value);
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
  public ReactiveCommand<Unit, Unit> ExportImageCommand { get; private set; } = null!;

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

    var canExportImage = this.WhenAnyValue(
      x => x.SelectedImage,
      x => x.SelectedImageIndex,
      (image, index) => image != null && index >= 0);

    PreviousFileCommand = ReactiveCommand.Create(LoadPreviousFile, canGoPrevious);
    NextFileCommand = ReactiveCommand.Create(LoadNextFile, canGoNext);
    ExportImageCommand = ReactiveCommand.CreateFromTask(ExportImageAsync, canExportImage);

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
      UpdateHeaderInfo();

      this.RaisePropertyChanged(nameof(CurrentFilePath));
      _notificationService.ShowSuccess($"Opened {System.IO.Path.GetFileName(filePath)}");
    }
    catch (Exception ex)
    {
      _logger.LogError(ex, "Failed to load TEX file: {FilePath}", filePath);
      _notificationService.ShowError($"Failed to load {System.IO.Path.GetFileName(filePath)}", ex);
    }
  }

  private void UpdateHeaderInfo()
  {
    if (_currentTexFile?.Header == null)
    {
      HeaderInfo = "No file loaded";
      return;
    }

    HeaderInfo = FormatHeaderInfo(_currentTexFile.Header);
  }

  private void UpdateSelectedImageHeaderInfo(int imageIndex)
  {
    if (imageIndex < 0 || imageIndex >= _texImages.Count)
    {
      SelectedImageHeaderInfo = "No image selected";
      return;
    }

    var texImage = _texImages[imageIndex];
    var info = new System.Text.StringBuilder();
    
    info.AppendLine($"Image #{imageIndex + 1}");
    info.AppendLine();
    info.Append(FormatHeaderInfo(texImage.Header));
    info.AppendLine();
    info.AppendLine($"Mipmap Count: {texImage.Mipmaps.Count()}");
    
    SelectedImageHeaderInfo = info.ToString();
  }

  private string FormatHeaderInfo(TexHeader header)
  {
    var info = new System.Text.StringBuilder();
    
    info.AppendLine($"Flags: 0x{(uint)header.Flags:X8}");
    info.AppendLine();
    
    // Flag details
    if (header.Flags.HasFlag(TexFlags.Rgba32))
      info.AppendLine("  • RGBA32 Format");
    if (header.Flags.HasFlag(TexFlags.Lod))
      info.AppendLine("  • LOD Support");
    if (header.Flags.HasFlag(TexFlags.Standard))
      info.AppendLine("  • Standard Texture");
    if (header.Flags.HasFlag(TexFlags.TgaSpriteSheet))
      info.AppendLine("  • Multi-sided");
    if (header.Flags.HasFlag(TexFlags.Animated))
      info.AppendLine("  • Animated");
    if (header.Flags.HasFlag(TexFlags.PreciseAlpha))
      info.AppendLine("  • RGBA 4-channel");
    if (header.Flags.HasFlag(TexFlags.Special))
      info.AppendLine("  • Special Texture");
    if (header.Flags.HasFlag(TexFlags.Mipmap))
      info.AppendLine("  • Mipmap Present");
    if (header.Flags.HasFlag(TexFlags.Cursor))
      info.AppendLine("  • Cursor Definition");
    if (header.Flags.HasFlag(TexFlags.SideColors))
      info.AppendLine("  • Side Color");
    if (header.Flags.HasFlag(TexFlags.DamageStates))
      info.AppendLine("  • Destroyed States");
    if (header.Flags.HasFlag(TexFlags.Container))
      info.AppendLine("  • Container");
    
    info.AppendLine();
    
    if (header.Width > 0 || header.Height > 0)
    {
      info.AppendLine($"Dimensions: {header.Width} x {header.Height}");
    }
    
    if (header.SlideCount > 1)
    {
      info.AppendLine($"Slide Count: {header.SlideCount}");
    }
    
    if (header.DestroyedCount > 1)
    {
      info.AppendLine($"Destroyed Count: {header.DestroyedCount}");
    }
    
    if (header.LodCount > 0)
    {
      info.AppendLine($"LOD Levels: {header.LodCount}");
    }
    
    if (header.Flags.HasFlag(TexFlags.Cursor))
    {
      info.AppendLine($"Cursor: ({header.CursorX}, {header.CursorY})");
      info.AppendLine($"Animation Type: {header.CursorAnimationType}");
      info.AppendLine($"Frame Time: {header.CursorFrameTime}");
    }
    
    if (header.Magic > 0)
    {
      info.AppendLine($"Magic: 0x{header.Magic:X4}");
    }
    
    return info.ToString();
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
    _texImages.Clear();

    if (_currentTexFile == null)
      return;

    foreach (var imageGroup in _currentTexFile.Images)
    {
      foreach (var texImage in imageGroup)
      {
        // Store the TexImage for header access
        _texImages.Add(texImage);

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

  private async Task ExportImageAsync()
  {
    try
    {
      if (_selectedImageIndex < 0 || _selectedImageIndex >= _texImages.Count)
      {
        _notificationService.ShowWarning("No image selected");
        return;
      }

      var texImage = _texImages[_selectedImageIndex];
      var firstMipmap = texImage.Mipmaps.FirstOrDefault();
      if (firstMipmap == null)
      {
        _notificationService.ShowWarning("No mipmap available for export");
        return;
      }

      var fileName = string.IsNullOrEmpty(_currentFilePath)
        ? $"image_{_selectedImageIndex}.png"
        : $"{System.IO.Path.GetFileNameWithoutExtension(_currentFilePath)}_{_selectedImageIndex}.png";

      var filePath = await _dialogService.ShowSaveFileDialogAsync(
        "Export Image",
        fileName,
        ("PNG Files", "*.png"));

      if (string.IsNullOrEmpty(filePath))
        return;

      using (var image = SKImage.FromBitmap(firstMipmap))
      using (var data = image.Encode(SKEncodedImageFormat.Png, 100))
      using (var stream = File.OpenWrite(filePath))
      {
        data.SaveTo(stream);
      }

      _notificationService.ShowSuccess($"Image exported to {System.IO.Path.GetFileName(filePath)}");
      _logger.LogInformation("Exported image to: {FilePath}", filePath);
    }
    catch (Exception ex)
    {
      _logger.LogError(ex, "Failed to export image");
      _notificationService.ShowError("Failed to export image", ex);
    }
  }
}
