using EarthTool.Common;
using EarthTool.TEX.Interfaces;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace EarthTool.TEX
{
  public class TexFile : ITexFile
  {
    public bool HasHeader => Header != null;
    public TexHeader Header { get; private set; }
    public IEnumerable<IEnumerable<TexImage>> Images { get; }

    public TexFile(BinaryReader reader)
    {
      Images = Read(reader);
    }

    private IEnumerable<IEnumerable<TexImage>> Read(BinaryReader reader)
    {
      var images = new List<List<TexImage>>();
      IsValidModel(reader);
      var header = new TexHeader(reader);
      if (header.SubType.HasFlag(TextureSubType.Grouped) ||
          header.SubType.HasFlag(TextureSubType.Collection) ||
          header.SubType.HasFlag(TextureSubType.Sides))
      {
        Header = header;

        var group = new List<List<TexImage>>();
        for (int i = 0; i < Math.Max(header.GroupCount, 1); i++)
        {
          var groupImages = new List<TexImage>();
          for (int j = 0; j < Math.Max(header.ElementCount, 1); j++)
          {
            groupImages.AddRange(Read(reader).SelectMany(i => i));
          }
          group.Add(groupImages);
        }
        images.AddRange(group);
      }
      else
      {
        images.Add(new List<TexImage>() { new TexImage(header, reader) });
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