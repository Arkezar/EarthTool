using System;
using System.Collections.Generic;
using System.Text;

namespace WDExtract.Resources
{
  public class Interface : ResourceTranslatable
  {
    public Interface(string filename, (uint, uint, uint) fileInfo, string translationId) : base(filename, fileInfo, translationId)
    {
    }
  }
}
