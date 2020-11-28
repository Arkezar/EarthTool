using System;
using System.Collections.Generic;
using System.Text;

namespace WDExtract.Resources
{
  public class Group : Resource
  {
    public Group(string filename, (uint, uint, uint) fileInfo, byte[] data) : base(filename, fileInfo, data)
    {
    }
  }
}
