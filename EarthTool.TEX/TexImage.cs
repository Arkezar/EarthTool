using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;

namespace EarthTool.TEX
{
  public class TexImage
  {
    public TexHeader Header { get; }
    public IEnumerable<Image> Mipmaps { get; }

    public TexImage(TexHeader header, BinaryReader reader)
    {
      Header = header;
      Mipmaps = LoadMipmaps(reader);
    }

    private IEnumerable<Image> LoadMipmaps(BinaryReader reader)
    {
      var images = new List<Image>();
      var width = Header.Width;
      var height = Header.Height;

      do
      {
        var image = new Bitmap(width, height);
        for (var w = 0; w < image.Width; w++)
        {
          for (var h = 0; h < image.Height; h++)
          {
            var red = reader.ReadByte();
            var green = reader.ReadByte();
            var blue = reader.ReadByte();
            var alpha = reader.ReadByte();
            var color = Color.FromArgb(alpha, red, green, blue);
            image.SetPixel(h, w, color);
          }
        }

        images.Add(image);
        width /= 2;
        height /= 2;
      } while (images.Count < Math.Max(Header.LodLevels, 1));

      return images;
    }
  }
}