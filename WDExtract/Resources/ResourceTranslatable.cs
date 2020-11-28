using System;
using System.Collections.Generic;
using System.Text;

namespace WDExtract.Resources
{
  public abstract class ResourceTranslatable : Resource
  {
    public string TranslationId
    {
      get;
    }

    protected ResourceTranslatable(string filename, (uint, uint, uint) fileInfo, string translationId, byte[] data = null) : base(filename, fileInfo, data)
    {
      TranslationId = translationId;
    }


    public override string ToString()
    {
      return $"{base.ToString()} TransaltionID: {TranslationId}";
    }
  }
}
