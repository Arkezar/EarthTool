using EarthTool.Common.Enums;
using EarthTool.Common.Interfaces;
using System;
using System.Linq;
using System.Text;

namespace EarthTool.WD.Models;

public class ArchiveItem(string fileName, IEarthInfo header, ReadOnlyMemory<byte> data, int decompressedSize)
  : IArchiveItem
{
  public string FileName { get; } = fileName;
  public IEarthInfo Header { get; } = header;
  public int CompressedSize => data.Length;
  public int DecompressedSize { get; } = decompressedSize;

  public byte[] Extract(IDecompressor decompressor, Encoding encoding)
  {
    if (!Header.Flags.HasFlag(FileFlags.Compressed))
    {
      var header = Header.ToByteArray(encoding);
      return header.Concat(data.ToArray()).ToArray();
    }
    else
    {
      var extractHeader = (IEarthInfo)Header.Clone();
      extractHeader.RemoveFlag(FileFlags.Compressed);
      var header = extractHeader.ToByteArray(encoding);
      return header.Concat(decompressor.Decompress(data.ToArray())).ToArray();
    }
  }

  public int CompareTo(IArchiveItem other)
  {
    if (ReferenceEquals(this, other)) return 0;
    if (other is null) return 1;
    return string.Compare(FileName, other.FileName, StringComparison.Ordinal);
  }
}