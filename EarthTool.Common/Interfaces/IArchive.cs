using System;
using System.Collections.Generic;
using System.Text;

namespace EarthTool.Common.Interfaces
{
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