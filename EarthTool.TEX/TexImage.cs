using System.Collections.Generic;
using System.Drawing;
using System.IO;

namespace EarthTool.TEX
{
  public class TexImage
  {
    public TexHeader Header { get; }
    public int Width { get; }
    public int Height { get; }
    public int LodLevels { get; }
    public byte[] Unknown { get; }
    public IEnumerable<Image> Mipmaps { get; }

    public TexImage(TexHeader header, int width, int height, int lodLevels, byte[] unknown, BinaryReader reader)
    {
      Header = header;
      Width = width;
      Height = height;
      LodLevels = lodLevels;
      Unknown = unknown;
      Mipmaps = LoadMipmaps(reader);
    }

    private IEnumerable<Image> LoadMipmaps(BinaryReader reader)
    {
      var images = new List<Image>();
      var width = Width;
      var height = Height;

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
      } while (images.Count < LodLevels);

      return images;
    }
  }
}