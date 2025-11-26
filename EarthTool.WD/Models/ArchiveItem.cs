using EarthTool.Common.Enums;
using EarthTool.Common.Interfaces;
using EarthTool.WD.Interfaces;
using System;

namespace EarthTool.WD.Models;

public class ArchiveItem : IArchiveItem
{
  private readonly IArchiveDataSource _dataSource;
  private bool _disposed;

  public ArchiveItem(string fileName, IEarthInfo header, IArchiveDataSource dataSource, int compressedSize, int decompressedSize)
  {
    _dataSource = dataSource;
    FileName = fileName;
    Header = header;
    CompressedSize = compressedSize;
    DecompressedSize = decompressedSize;
  }

  public string FileName { get; }
  public IEarthInfo Header { get; }
  public int CompressedSize { get; }
  public int DecompressedSize { get; }
  public bool IsCompressed => Header.Flags.HasFlag(FileFlags.Compressed);
  public ReadOnlyMemory<byte> Data => _dataSource.Data;

  public int CompareTo(IArchiveItem other)
  {
    if (ReferenceEquals(this, other)) return 0;
    if (other is null) return 1;
    return string.Compare(FileName, other.FileName, StringComparison.OrdinalIgnoreCase);
  }

  public void Dispose()
  {
    if (_disposed)
    {
      return;
    }

    _dataSource?.Dispose();
    _disposed = true;
  }
}
