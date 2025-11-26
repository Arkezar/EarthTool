using System;
using System.IO;

namespace EarthTool.Common.Interfaces
{
  public interface IDecompressor
  {
    byte[] Decompress(ReadOnlySpan<byte> data);
    byte[] Decompress(byte[] data);
    byte[] Decompress(Stream stream);
    Stream OpenDecompressionStream(Stream stream, bool leaveOpen = false);

  }
}
