using System;
using System.Collections.Generic;
using System.Text;

namespace WDExtract.Resources
{
  public class Interface : Resource
  {
    public string TranslationId
    {
      get;
    }

    public Interface(string filename, (uint, uint, uint) fileInfo, string translationId) : base(filename, fileInfo)
    {
      TranslationId = translationId;
    }

    public override string ToString()
    {
      return $"{base.ToString()} TransaltionID: {TranslationId}";
    }
  }
}
