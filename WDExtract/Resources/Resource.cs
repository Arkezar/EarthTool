using System;
using System.Collections.Generic;
using System.Text;

namespace WDExtract.Resources
{
  public class Resource
  {
    public string Filename
    {
      get;
    }

    public uint Offset
    {
      get;
    }

    public uint Length
    {
      get;
    }

    public uint DecompressedLength
    {
      get;
    }

    public Resource(string filename, (uint, uint, uint) fileInfo)
    {
      Filename = filename;
      Offset = fileInfo.Item1;
      Length = fileInfo.Item2;
      DecompressedLength = fileInfo.Item3;
    }

    public override string ToString()
    {
      return $"{Filename} Offset: {Offset} Compressed: {Length} Uncompressed: {DecompressedLength}";
    }
  }
}
