using System.IO;

namespace EarthTool.Common.Extensions
{
  public static class StreamExtensions
  {
    public static byte[] ReadBytes(this Stream stream, int count)
    {
      var buffer = new byte[count];
      stream.Read(buffer, 0, count);
      return buffer;
    }
  }
}