using System.IO;

namespace MESHConverter.Extensions
{
  public static class StreamExtensions
  {
    public static byte[] ReadBytes(this Stream stream, int count, int offset = 0)
    {
      var buffer = new byte[count];
      stream.Read(buffer, offset, count);
      return buffer;
    }
  }
}
