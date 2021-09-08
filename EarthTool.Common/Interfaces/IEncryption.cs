using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace EarthTool.Common.Interfaces
{
  public interface IEncryption
  {
    byte[] Decompress(byte[] data);
    byte[] Decompress(Stream stream);
    byte[] Compress(byte[] data);
    byte[] Compress(Stream stream);
  }
}
