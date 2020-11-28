using System;
using System.Collections.Generic;
using System.Text;

namespace WDExtract.Resources
{
  public class Group : Resource
  {
    public byte[] Data
    {
      get;
    }

    public Group(string filename, (uint, uint, uint) fileInfo, byte[] data) : base(filename, fileInfo)
    {
      Data = data;
    }
  }
}
