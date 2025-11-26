using System;
using System.Collections.Generic;
using System.Text;

namespace EarthTool.Common.Interfaces
{
  /// <summary>
  /// Represents an archive file container.
  /// Implements IDisposable to properly release memory-mapped file resources.
  /// </summary>
  public interface IArchive : IDisposable
  {
    IEarthInfo Header { get; }
    DateTime LastModification { get; }
    IReadOnlyCollection<IArchiveItem> Items { get; }
    void AddItem(IArchiveItem item);
    void RemoveItem(IArchiveItem item);
    byte[] ToByteArray(ICompressor compressor, Encoding encoding);
  }
}
