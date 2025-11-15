using System;
using System.Text;

namespace EarthTool.Common.Interfaces
{
  public interface IArchiveItem : IComparable<IArchiveItem>
  {
    string FileName { get; }
    IEarthInfo Header { get; }
    int CompressedSize { get; }
    int DecompressedSize { get; }
    byte[] Extract(IDecompressor decompressor, Encoding encoding);
  }
}