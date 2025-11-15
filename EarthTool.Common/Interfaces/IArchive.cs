using System;
using System.Collections.Generic;
using System.Text;

namespace EarthTool.Common.Interfaces
{
  public interface IArchive
  {
    IEarthInfo Header { get; }
    DateTime LastModified { get; }
    IReadOnlyCollection<IArchiveItem> Items { get; }
    void AddItem(IArchiveItem item);
    void RemoveItem(IArchiveItem item);
    byte[] ToByteArray(ICompressor compressor, Encoding encoding);
  }
}