using EarthTool.Common.Interfaces;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;

namespace EarthTool.TEX
{
  public class TEXConverter : ITEXConverter
  {
    private readonly ILogger<TEXConverter> _logger;

    public TEXConverter(ILogger<TEXConverter> logger)
    {
      _logger = logger;
    }

    public int Convert(string filePath, string outputPath = null)
    {
      outputPath ??= Path.GetDirectoryName(filePath);

      var data = File.ReadAllBytes(filePath);
      var filename = Path.GetFileNameWithoutExtension(filePath);
      using (var stream = new MemoryStream(data))
      {
        var header = ReadHeader(stream);
        if (header.NumberOfMaps > 0)
        {
          for (var i = 0; i < header.NumberOfMaps; i++)
          {
            var t = ReadHeader(stream);
            var images = ReadBitmap(stream, t.Type, t.Subtype);
            foreach (var image in images)
            {
              SaveBitmap(outputPath, filename, i, image);
            }
          }
        }
        else
        {
          var images = ReadBitmap(stream, header.Type, header.Subtype);
          foreach (var image in images)
          {
            SaveBitmap(outputPath, filename, 0, image);
          }
        }
      }
      return 1;
    }

    private static void SaveBitmap(string workDir, string filename, int i, Image image)
    {
      var outputDir = Path.Combine(workDir, filename);
      if (!Directory.Exists(outputDir))
      {
        Directory.CreateDirectory(outputDir);
      }

      image.Save(Path.Combine(outputDir, $"{filename}_{i}_{image.Width}x{image.Height}.png"));
    }

    private static IEnumerable<Image> ReadBitmap(Stream stream, int type, int subtype)
    {
      int infoLength;
      switch (type)
      {
        case 6:
        case 38:
          infoLength = 12;
          break;
        case 34:
          infoLength = subtype > 0 ? 24 : 8;
          break;
        default:
          infoLength = 8;
          break;
      }

      var images = new List<Image>();

      var dimensions = new byte[infoLength];
      stream.Read(dimensions, 0, infoLength);

      var width = BitConverter.ToInt32(dimensions, 0);
      var height = BitConverter.ToInt32(dimensions, 4);


      var numberOfMipmaps = 1;
      if (type == 38 || type == 6)
      {
        numberOfMipmaps = BitConverter.ToInt32(dimensions, 8);
      }

      do
      {
        var image = new Bitmap(width, height);
        for (var w = 0; w < image.Width; w++)
        {
          for (var h = 0; h < image.Height; h++)
          {
            var red = stream.ReadByte();
            var green = stream.ReadByte();
            var blue = stream.ReadByte();
            var alpha = stream.ReadByte();
            var color = Color.FromArgb(alpha, red, green, blue);
            image.SetPixel(h, w, color);
          }
        }
        images.Add(image);
        width /= 2;
        height /= 2;
      } while (images.Count < numberOfMipmaps);

      return images;
    }

    private static Header ReadHeader(Stream stream)
    {
      var buffer = new byte[16];
      stream.Read(buffer, 0, 16);
      if (buffer[0] != 84 && buffer[1] != 69 && buffer[2] != 88)
      {
        throw new Exception("Invalid header");
      }

      var numberOfMaps = buffer[11] == 128 || buffer[11] == 67 || buffer[11] == 16 ? BitConverter.ToInt32(buffer, 12) : 0;
      if (buffer[11] == 192)
      {
        var tmpBuffer = new byte[4];
        stream.Read(tmpBuffer, 0, 4);
        numberOfMaps = BitConverter.ToInt32(buffer, 12) * BitConverter.ToInt32(tmpBuffer, 0);
      }

      return new Header(buffer[8], buffer[10], numberOfMaps);
    }
  }
}
