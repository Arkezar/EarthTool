using SkiaSharp;
using System.Collections.Generic;
using System.IO;

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
      } while (images.Count < Header.LodCount);
      return images;
    }
  }
}
