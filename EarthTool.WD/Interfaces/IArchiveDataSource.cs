using System;

namespace EarthTool.WD.Interfaces;

/// <summary>
/// Provides access to archive item data.
/// Implementations may use different strategies (in-memory, memory-mapped).
/// </summary>
public interface IArchiveDataSource : IDisposable
{
  ReadOnlyMemory<byte> Data { get; }
}
