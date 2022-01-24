using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace EarthTool.Common.Interfaces
{
  public interface ICompressor
  {
    byte[] Compress(byte[] data);
    byte[] Compress(Stream stream);
  }
}
