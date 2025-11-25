using EarthTool.Common.Enums;
using EarthTool.Common.Interfaces;
using ReactiveUI;
using System;
using System.Collections.ObjectModel;

namespace EarthTool.WD.GUI.ViewModels;

/// <summary>
/// ViewModel for tree structure representing files and folders in archive.
/// </summary>
public class TreeItemViewModel : ViewModelBase
{
  private bool _isExpanded;

  public TreeItemViewModel(string name, bool isFolder)
  {
    Name = name;
    IsFolder = isFolder;
    Children = new ObservableCollection<TreeItemViewModel>();
  }

  /// <summary>
  /// Gets the name of the file or folder.
  /// </summary>
  public string Name { get; }

  /// <summary>
  /// Gets whether this is a folder (true) or file (false).
  /// </summary>
  public bool IsFolder { get; }

  /// <summary>
  /// Gets the underlying archive item (null for folders).
  /// </summary>
  public IArchiveItem? Item { get; init; }

  /// <summary>
  /// Gets the full path of this item.
  /// </summary>
  public string FullPath { get; init; } = string.Empty;

  /// <summary>
  /// Gets the children of this item.
  /// </summary>
  public ObservableCollection<TreeItemViewModel> Children { get; }

  /// <summary>
  /// Gets or sets whether this node is expanded.
  /// </summary>
  public bool IsExpanded
  {
    get => _isExpanded;
    set
    {
      this.RaiseAndSetIfChanged(ref _isExpanded, value);
      System.Diagnostics.Debug.WriteLine($"TreeItem '{Name}' expanded state changed to: {value}");
    }
  }

  /// <summary>
  /// Gets the compressed size in bytes (0 for folders).
  /// </summary>
  public int CompressedSize => Item?.CompressedSize ?? 0;

  /// <summary>
  /// Gets the decompressed size in bytes (0 for folders).
  /// </summary>
  public int DecompressedSize => Item?.DecompressedSize ?? 0;

  /// <summary>
  /// Gets whether the item is compressed.
  /// </summary>
  public bool IsCompressed => Item?.IsCompressed ?? false;

  /// <summary>
  /// Gets the file flags (None for folders).
  /// </summary>
  public FileFlags Flags => Item?.Header.Flags ?? FileFlags.None;

  /// <summary>
  /// Gets whether this item has the Text flag set.
  /// </summary>
  public bool HasTextFlag => Item?.Header.Flags.HasFlag(FileFlags.Text) ?? false;

  /// <summary>
  /// Gets the resource GUID (null for folders).
  /// </summary>
  public Guid? ResourceGuid => Item?.Header.Guid;

  /// <summary>
  /// Gets the resource type (null for folders).
  /// </summary>
  public ResourceType? ResourceType => Item?.Header.ResourceType;

  /// <summary>
  /// Gets the translation ID/named resource identifier (empty for folders).
  /// </summary>
  public string TranslationId => Item?.Header.TranslationId ?? string.Empty;

  /// <summary>
  /// Gets the compression ratio as a percentage.
  /// </summary>
  public double CompressionRatio
  {
    get
    {
      if (IsFolder || DecompressedSize == 0) return 0;
      return (1.0 - (double)CompressedSize / DecompressedSize) * 100;
    }
  }

  /// <summary>
  /// Gets a formatted string for the compressed size.
  /// </summary>
  public string FormattedCompressedSize => IsFolder ? "-" : FormatBytes(CompressedSize);

  /// <summary>
  /// Gets a formatted string for the decompressed size.
  /// </summary>
  public string FormattedDecompressedSize => IsFolder ? "-" : FormatBytes(DecompressedSize);

  /// <summary>
  /// Gets a formatted string for the compression ratio.
  /// </summary>
  public string FormattedCompressionRatio => IsFolder ? "-" : (IsCompressed ? $"{CompressionRatio:F1}%" : "N/A");

  /// <summary>
  /// Gets a formatted string for the resource GUID.
  /// </summary>
  public string FormattedResourceGuid => IsFolder ? "-" : (ResourceGuid?.ToString() ?? "N/A");

  /// <summary>
  /// Gets a formatted string for the resource type.
  /// </summary>
  public string FormattedResourceType => IsFolder ? "-" : (ResourceType?.ToString() ?? "N/A");

  /// <summary>
  /// Gets a formatted string for the translation ID.
  /// </summary>
  public string FormattedTranslationId => IsFolder ? "-" : (string.IsNullOrEmpty(TranslationId) ? "N/A" : TranslationId);

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

  public override string ToString() => Name;
}
