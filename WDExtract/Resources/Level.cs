using System;
using System.Collections.Generic;
using System.Text;

namespace WDExtract.Resources
{
  public class Level : Resource
  {
    public string Id
    {
      get;
    }

    public Level(string filename, (uint, uint, uint) fileInfo, string id, byte[] data) : base(filename, fileInfo, data)
    {
      Id = id;
    }
  }
}
