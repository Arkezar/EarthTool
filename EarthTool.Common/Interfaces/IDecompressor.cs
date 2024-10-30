using System.IO;

namespace EarthTool.Common.Interfaces
{
  public interface IDecompressor
  {
    byte[] Decompress(byte[] data);
    byte[] Decompress(Stream stream);
  }
}