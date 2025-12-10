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
      Header = new TexHeader(reader);
      if (Header.Flags.HasFlag(TexFlags.TEX_FLAG_CONTAINER) || Header.Flags.HasFlag(TexFlags.TEX_FLAG_DESTROYED) || Header.Flags.HasFlag(TexFlags.TEX_FLAG_SIDECOLOR))
      {
        for(var i = 0; i < Header.SlideCount * Header.DestroyedCount; i++)
        {
          IsValidModel(reader);
          var slideHeader = new TexHeader(reader);
          images.Add(new List<TexImage>() {new TexImage(slideHeader, reader)});
        }
      }
      else
      {
        images.Add(new List<TexImage>() {new TexImage(Header, reader)});
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
