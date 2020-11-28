using System;
using System.Collections.Generic;
using System.Text;

namespace WDExtract.Resources
{
  public class Terrain : Resource
  {
    public string TranslationId
    {
      get;
    }

    public byte[] Data
    {
      get;
    }

    public Terrain(string filename, (uint, uint, uint) fileInfo, string transaltionId, byte[] data) : base(filename, fileInfo)
    {
      TranslationId = transaltionId;
      Data = data;
    }
  }
}
