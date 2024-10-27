using EarthTool.Common;
using System;
using System.Collections.Generic;
using System.IO;

namespace EarthTool.TEX
{
  public class TexFile
  {
    public bool HasHeader => Header != null;
    public TexHeader Header { get; private set; }
    public IEnumerable<TexImage> Images { get; }

    public TexFile(BinaryReader reader)
    {
      Images = Read(reader);
    }
    
    private IEnumerable<TexImage> Read(BinaryReader reader)
    {
      var images = new List<TexImage>();
      IsValidModel(reader);
      var header = new TexHeader(reader);
      if (header.NumberOfMaps > 0)
      {
        Header = header;
        for (int i = 0; i < header.NumberOfMaps; i++)
        {
          images.AddRange(Read(reader));
        }
      }
      else
      {
        var width = reader.ReadInt32();
        var height = reader.ReadInt32();
        var lodLevels = 1;
        var unknown = new byte[12];
        if (header.Type == 6 || header.Type == 38)
        {
          lodLevels = reader.ReadInt32();
        }

        if (header.Type == 34 && header.Subtype > 0)
        {
          unknown = reader.ReadBytes(12);
        }

        images.Add(new TexImage(header, width, height, lodLevels, unknown, reader));
      }

      return images;
    }
    
    private void IsValidModel(BinaryReader reader)
    {
      var valid = reader.ReadBytes(Identifiers.Texture.Length).AsSpan().SequenceEqual(Identifiers.Texture);
      if (!valid)
      {
        throw new NotSupportedException("Unhandled file format");
      }
    }
  }
}