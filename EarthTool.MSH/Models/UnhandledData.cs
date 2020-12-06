using EarthTool.Common.Extensions;
using System;
using System.IO;

namespace EarthTool.MSH.Models
{
  public class UnhandledData
  {
    public UnhandledData(Stream stream, int fields, int fieldSize)
    {
      var length = BitConverter.ToInt32(stream.ReadBytes(4));
      stream.ReadBytes(length * fields * fieldSize);
    }
  }
}
