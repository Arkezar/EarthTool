using System;

namespace EarthTool.WD.Interfaces;

public interface IArchiveDataSource
{
  ReadOnlyMemory<byte> Data { get; }
}