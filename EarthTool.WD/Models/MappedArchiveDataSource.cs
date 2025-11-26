using EarthTool.WD.Interfaces;
using System;
using System.IO.MemoryMappedFiles;

namespace EarthTool.WD.Models;

/// <summary>
/// Memory-mapped file data source for archive items.
/// Note: Does NOT own the MemoryMappedFile - it's shared among all items.
/// Uses lazy initialization to cache data on first access.
/// </summary>
public class MappedArchiveDataSource : IArchiveDataSource
{
  private readonly MemoryMappedFile _file;
  private readonly int _offset;
  private readonly int _length;
  private readonly Lazy<byte[]> _cachedData;

  public MappedArchiveDataSource(MemoryMappedFile file, int offset, int length)
  {
    _file = file ?? throw new ArgumentNullException(nameof(file));
    _offset = offset;
    _length = length;
    _cachedData = new Lazy<byte[]>(LoadData);
  }

  public ReadOnlyMemory<byte> Data => _cachedData.Value;

  private byte[] LoadData()
  {
    using var accessor = CreateAccessor();
    var buffer = new byte[_length];
    accessor.ReadArray(_offset, buffer, 0, _length);
    return buffer;
  }

  private MemoryMappedViewAccessor CreateAccessor()
    => _file.CreateViewAccessor(0, 0, MemoryMappedFileAccess.Read);

  /// <summary>
  /// Does NOT dispose the MemoryMappedFile as it's shared.
  /// The owning Archive is responsible for disposing the MMF.
  /// </summary>
  public void Dispose()
  {
    // Intentionally empty - we don't own the MemoryMappedFile
    // Archive owns it and will dispose it
  }
}
