using System;
using System.Collections.Generic;
using System.Text;

namespace WDExtract.Resources
{
  public class Mesh : Resource
  {
    public Mesh(string filename, (uint, uint, uint) fileInfo, byte[] data) : base(filename, fileInfo, data)
    {
    }
  }
}
