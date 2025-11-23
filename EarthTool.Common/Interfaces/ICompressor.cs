using System.IO;

namespace EarthTool.Common.Interfaces
{
  public interface ICompressor
  {
    byte[] Compress(byte[] data);
    byte[] Compress(Stream stream);
    Stream OpenCompressionStream(Stream stream, bool leaveOpen = false);
  }
}