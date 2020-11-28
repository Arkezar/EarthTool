using System;
using System.Collections.Generic;
using System.Text;

namespace WDExtract.Resources
{
  public class Terrain : ResourceTranslatable
  {
    public Terrain(string filename, (uint, uint, uint) fileInfo, string transaltionId, byte[] data) : base(filename, fileInfo, transaltionId, data)
    {
    }
  }
}
