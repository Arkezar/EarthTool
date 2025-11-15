using EarthTool.WD.Interfaces;
using System;
using System.IO.MemoryMappedFiles;

namespace EarthTool.WD.Models;

public class MappedArchiveDataSource : IArchiveDataSource
{
  private readonly MemoryMappedFile _file;
  private readonly int              _offset;
  private readonly int              _length;

  public MappedArchiveDataSource(MemoryMappedFile file, int offset, int length)
  {
    _file = file;
    _offset = offset;
    _length = length;
  }

  public ReadOnlyMemory<byte> Data
  {
    get
    {
      using var accessor = CreateAccessor();
      var buffer = new byte[_length];
      accessor.ReadArray(0, buffer, 0, _length);
      return buffer;
    }
  }

  private MemoryMappedViewAccessor CreateAccessor()
    => _file.CreateViewAccessor(_offset, _length, MemoryMappedFileAccess.Read);
}