using EarthTool.WD.Interfaces;
using System;

namespace EarthTool.WD.Models;

/// <summary>
/// In-memory data source for archive items.
/// Used for newly created archives or small files.
/// </summary>
public class InMemoryArchiveDataSource : IArchiveDataSource
{
  public InMemoryArchiveDataSource(byte[] data)
  {
    Data = data ?? throw new ArgumentNullException(nameof(data));
  }

  public ReadOnlyMemory<byte> Data { get; }

  public void Dispose()
  {
    // No resources to dispose - data is managed by GC
  }
}
