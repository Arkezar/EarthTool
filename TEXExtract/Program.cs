using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;

namespace TEXExtract
{
  class Program
  {
    static void Main(string[] args)
    {
      var file = args[0];
      var data = File.ReadAllBytes(file);

      var workDir = Path.GetDirectoryName(file);
      var filename = Path.GetFileNameWithoutExtension(file);
      Console.WriteLine("Extracting " + filename);
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
              SaveBitmap(workDir, filename, i, image);
            }
          }
        }
        else
        {
          var images = ReadBitmap(stream, header.Type, header.Subtype);
          foreach (var image in images)
          {
            SaveBitmap(workDir, filename, 0, image);
          }
        }
      }

      Console.WriteLine("Finished!");
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
        for (var h = 0; h < image.Height; h++)
        {
          for (var w = 0; w < image.Width; w++)
          {
            var red = stream.ReadByte();
            var green = stream.ReadByte();
            var blue = stream.ReadByte();
            var alpha = stream.ReadByte();
            var color = Color.FromArgb(alpha, red, green, blue);
            image.SetPixel(w, h, color);
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
        numberOfMaps = BitConverter.ToInt32(buffer, 12) * BitConverter.ToInt32(tmpBuffer);
      }

      return new Header(buffer[8], buffer[10], numberOfMaps);
    }
  }
}
