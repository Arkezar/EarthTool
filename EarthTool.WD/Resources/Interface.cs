using System;
using System.Collections.Generic;
using System.Text;

namespace EarthTool.WD.Resources
{
  public class Interface : TranslatableResource
  {
    public Interface(string filename, (uint, uint, uint) fileInfo, string translationId) : base(filename, fileInfo, translationId)
    {
    }
  }
}
