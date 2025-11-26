using EarthTool.Common.Enums;
using EarthTool.Common.Interfaces;
using ReactiveUI;
using System;

namespace EarthTool.WD.GUI.ViewModels;

/// <summary>
/// ViewModel wrapper for IArchiveItem.
/// </summary>
public class ArchiveItemViewModel : ViewModelBase
{
  private readonly IArchiveItem _item;

  public ArchiveItemViewModel(IArchiveItem item)
  {
    _item = item ?? throw new ArgumentNullException(nameof(item));
  }

  /// <summary>
  /// Gets the underlying archive item.
  /// </summary>
  public IArchiveItem Item => _item;

  /// <summary>
  /// Gets the file name.
  /// </summary>
  public string FileName => _item.FileName;

  /// <summary>
  /// Gets the compressed size in bytes.
  /// </summary>
  public int CompressedSize => _item.CompressedSize;

  /// <summary>
  /// Gets the decompressed size in bytes.
  /// </summary>
  public int DecompressedSize => _item.DecompressedSize;

  /// <summary>
  /// Gets whether the item is compressed.
  /// </summary>
  public bool IsCompressed => _item.IsCompressed;

  /// <summary>
  /// Gets the file flags.
  /// </summary>
  public FileFlags Flags => _item.Header.Flags;

  /// <summary>
  /// Gets the compression ratio as a percentage.
  /// </summary>
  public double CompressionRatio
  {
    get
    {
      if (DecompressedSize == 0) return 0;
      return (1.0 - (double)CompressedSize / DecompressedSize) * 100;
    }
  }

  /// <summary>
  /// Gets a formatted string for the compressed size.
  /// </summary>
  public string FormattedCompressedSize => FormatBytes(CompressedSize);

  /// <summary>
  /// Gets a formatted string for the decompressed size.
  /// </summary>
  public string FormattedDecompressedSize => FormatBytes(DecompressedSize);

  /// <summary>
  /// Gets a formatted string for the compression ratio.
  /// </summary>
  public string FormattedCompressionRatio => IsCompressed ? $"{CompressionRatio:F1}%" : "N/A";

  private static string FormatBytes(int bytes)
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

  public override string ToString() => FileName;
}