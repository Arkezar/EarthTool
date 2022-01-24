using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace EarthTool.Common.Interfaces
{
  public interface IDecompressor
  {
    byte[] Decompress(byte[] data);
    byte[] Decompress(Stream stream);
  }
}
