using EarthTool.Common;
using SkiaSharp;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;

namespace EarthTool.TEX
{
  public class TexImage
  {
    public TexHeader Header { get; }
    public IEnumerable<SKBitmap> Mipmaps { get; }

    public TexImage(TexHeader header, BinaryReader reader)
    {
      Header = header;
      Mipmaps = LoadMipmaps(reader);
    }

    private IEnumerable<SKBitmap> LoadMipmaps(BinaryReader reader)
    {
      var images = new List<SKBitmap>();
      var width = Header.Width;
      var height = Header.Height;

      do
      {
        var image = new SKBitmap(width, height);
        for (var h = 0; h < image.Height; h++)
        {
          for (var w = 0; w < image.Width; w++)
          {
            var red = reader.ReadByte();
            var green = reader.ReadByte();
            var blue = reader.ReadByte();
            var alpha = reader.ReadByte();
            var color = new SKColor(red, green, blue, alpha);
            image.SetPixel(w, h, color);
          }
        }

        images.Add(image);
        width /= 2;
        height /= 2;
      } while ((width > 0 || height > 0) && HasMoreMaps(reader) && reader.BaseStream.Position < reader.BaseStream.Length);
      return images;
    }

    private bool HasMoreMaps(BinaryReader reader)
    {
      byte[] buffer = Array.Empty<byte>();
      try
      {
        buffer = reader.ReadBytes(Identifiers.Texture.Length);
        return !buffer.SequenceEqual(Identifiers.Texture);
      }
      finally
      {
        reader.BaseStream.Seek(-buffer.Length, SeekOrigin.Current);
      }
    }
  }
}
