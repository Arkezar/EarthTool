using System;
using System.Drawing;
using System.IO;
using System.Linq;

namespace TEXExtract
{
  class Program
  {
    static void Main(string[] args)
    {
      //var file = args[0];
      var file = @"D:\Earth 2150 EftbP\WDFiles\Interface\Interface\compassUCS.tex";
      var data = File.ReadAllBytes(file);

      var workDir = Path.GetDirectoryName(file);
      var filename = Path.GetFileNameWithoutExtension(file);
      Console.WriteLine("Extracting " + filename);
      using (var stream = new MemoryStream(data))
      {
        var numberOfMaps = ReadHeader(stream);
        if (numberOfMaps > 0)
        {
          for (var i = 0; i < numberOfMaps; i++)
          {
            var t = ReadHeader(stream);
            var image = ReadBitmap(stream);
            SaveBitmap(workDir, filename, i, image);
          }
        }
        else
        {
          var image = ReadBitmap(stream);
          SaveBitmap(workDir, filename, 0, image);
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
      image.Save(Path.Combine(outputDir, $"{filename}_{i}.png"));
    }

    private static Image ReadBitmap(Stream stream)
    {
      var dimensions = new byte[8];
      stream.Read(dimensions, 0, 8);

      var width = BitConverter.ToInt32(dimensions, 0);
      var height = BitConverter.ToInt32(dimensions, 4);

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

      return image;
    }

    private static int ReadHeader(Stream stream)
    {
      var buffer = new byte[16];
      stream.Read(buffer, 0, 16);
      if(buffer[0] != 84 && buffer[1] != 69 && buffer[2] != 88)
      {
        throw new Exception("Inavlid header");
      }
      if (BitConverter.ToInt32(buffer, 8) <= -2147483616)
      {
        return BitConverter.ToInt32(buffer, 12);
      }
      else
      {
        return 0;
      }
    }
  }
}
