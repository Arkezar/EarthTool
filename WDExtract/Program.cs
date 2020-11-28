using Ionic.Zlib;
using System;
using System.IO;
using System.Linq;
using System.Text;
using WDExtract.Resources;

namespace WDExtract
{
  class Program
  {
    static void Main(string[] args)
    {
      if (args.Length < 1)
      {
        throw new ArgumentException("File path not provided");
      }

      var workDir = Path.GetDirectoryName(args[0]);
      var archiveName = Path.GetFileNameWithoutExtension(args[0]);
      var file = File.ReadAllBytes(args[0]);

      if (!IsValidWDFile(file))
      {
        throw new NotSupportedException("Unknown file format");
      }

      var dirByte = file.Skip(file.Length - 4).ToArray();
      var dirLn = (int)BitConverter.ToUInt32(dirByte, 0);
      var dirData = file.Skip(file.Length - dirLn).Take(dirLn).ToArray();

      var dir = Decompress(dirData);
      var dirdesc = new Directory(dir);

      foreach (var desc in dirdesc.Resources.Where(r => !(r is Group)))
      {
        Console.WriteLine(desc.ToString());
        var data = file.Skip((int)desc.Offset).Take((int)desc.Length).ToArray();

        if (data.Length > 0)
        {
          var fileDirectory = Path.GetDirectoryName(desc.Filename);

          if (!System.IO.Directory.Exists(Path.Combine(workDir, archiveName, fileDirectory)))
          {
            System.IO.Directory.CreateDirectory(Path.Combine(workDir, archiveName, fileDirectory));
          }

          data = desc.DecompressedLength == desc.Length ? data : Decompress(data);
          File.WriteAllBytes(Path.Combine(workDir, archiveName, desc.Filename), data);
          if (desc.HasUnknownData)
          {
            File.WriteAllBytes(Path.Combine(workDir, archiveName, desc.Filename) + ".unknownData", desc.UnknownData);
          }
          if (desc is ResourceTranslatable descTranslatable)
          {
            File.WriteAllText(Path.Combine(workDir, archiveName, desc.Filename) + ".translationId", descTranslatable.TranslationId);
          }
        }
      }

      Console.WriteLine("Finished extraction");
    }

    static bool IsValidWDFile(byte[] data)
    {
      var header = Decompress(data);
      return header.Take(8).SequenceEqual(new byte[] { 0xff, 0xa1, 0xd0, (byte)'1', (byte)'W', (byte)'D', 0x00, 0x02 });
    }

    static byte[] Decompress(byte[] compressedData)
    {
      using (var input = new MemoryStream(compressedData))
      {
        using (var output = new MemoryStream())
        {
          var decompressedData = new ZlibStream(input, CompressionMode.Decompress, false);
          decompressedData.CopyTo(output);

          return output.ToArray();
        }
      }
    }
  }
}
