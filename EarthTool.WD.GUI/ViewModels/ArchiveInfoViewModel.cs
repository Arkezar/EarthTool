using EarthTool.Common.Interfaces;
using ReactiveUI;
using System;

namespace EarthTool.WD.GUI.ViewModels;

/// <summary>
/// ViewModel for displaying archive information.
/// </summary>
public class ArchiveInfoViewModel : ViewModelBase
{
  private string? _filePath;
  private DateTime? _lastModification;
  private int _itemCount;
  private long _totalCompressedSize;
  private long _totalDecompressedSize;
  private IEarthInfo? _header;
  private string? _archiveGuid;

  public string? FilePath
  {
    get => _filePath;
    set
    {
      this.RaiseAndSetIfChanged(ref _filePath, value);
      this.RaisePropertyChanged(nameof(FormattedFilePath));
    }
  }

  public DateTime? LastModification
  {
    get => _lastModification;
    set
    {
      this.RaiseAndSetIfChanged(ref _lastModification, value);
      this.RaisePropertyChanged(nameof(FormattedLastModification));
    }
  }

  public int ItemCount
  {
    get => _itemCount;
    set
    {
      this.RaiseAndSetIfChanged(ref _itemCount, value);
      this.RaisePropertyChanged(nameof(FormattedItemCount));
    }
  }

  public long TotalCompressedSize
  {
    get => _totalCompressedSize;
    set
    {
      this.RaiseAndSetIfChanged(ref _totalCompressedSize, value);
      this.RaisePropertyChanged(nameof(FormattedTotalSize));
      this.RaisePropertyChanged(nameof(FormattedCompressionRatio));
    }
  }

  public long TotalDecompressedSize
  {
    get => _totalDecompressedSize;
    set
    {
      this.RaiseAndSetIfChanged(ref _totalDecompressedSize, value);
      this.RaisePropertyChanged(nameof(FormattedDecompressedSize));
      this.RaisePropertyChanged(nameof(FormattedCompressionRatio));
    }
  }

  public IEarthInfo? Header
  {
    get => _header;
    set => this.RaiseAndSetIfChanged(ref _header, value);
  }

  public string? ArchiveGuid
  {
    get => _archiveGuid;
    set
    {
      this.RaiseAndSetIfChanged(ref _archiveGuid, value);
      this.RaisePropertyChanged(nameof(FormattedArchiveGuid));
    }
  }

  public string FormattedFilePath => FilePath ?? "No archive loaded";

  public string FormattedLastModification => LastModification?.ToString("G") ?? "N/A";

  public string FormattedItemCount => $"{ItemCount} file(s)";

  public string FormattedTotalSize => FormatBytes(TotalCompressedSize);

  public string FormattedDecompressedSize => FormatBytes(TotalDecompressedSize);

  public string FormattedCompressionRatio
  {
    get
    {
      if (TotalDecompressedSize == 0) return "N/A";
      var ratio = (1.0 - (double)TotalCompressedSize / TotalDecompressedSize) * 100;
      return $"{ratio:F1}%";
    }
  }

  public string FormattedArchiveGuid => ArchiveGuid ?? "N/A";

  private static string FormatBytes(long bytes)
  {
    string[] sizes = { "B", "KB", "MB", "GB" };
    double len = bytes;
    int order = 0;
    while (len >= 1024 && order < sizes.Length - 1)
    {
      order++;
      len = len / 1024;
    }
    return $"{len:0.##} {sizes[order]}";
  }

  /// <summary>
  /// Updates the info from an archive.
  /// </summary>
  public void UpdateFromArchive(IArchive? archive, string? filePath = null)
  {
    if (archive == null)
    {
      Clear();
      return;
    }

    FilePath = filePath;
    LastModification = archive.LastModification;
    ItemCount = archive.Items.Count;
    Header = archive.Header;
    ArchiveGuid = archive.Header?.Guid.ToString();

    TotalCompressedSize = 0;
    TotalDecompressedSize = 0;
    foreach (var item in archive.Items)
    {
      TotalCompressedSize += item.CompressedSize;
      TotalDecompressedSize += item.DecompressedSize;
    }
  }

  /// <summary>
  /// Clears all information.
  /// </summary>
  public void Clear()
  {
    FilePath = null;
    LastModification = null;
    ItemCount = 0;
    TotalCompressedSize = 0;
    TotalDecompressedSize = 0;
    Header = null;
    ArchiveGuid = null;
  }
}