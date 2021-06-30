using EarthTool.Common.Interfaces;
using EarthTool.WD.Resources;
using Ionic.Zlib;
using Microsoft.Extensions.Logging;
using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace EarthTool.WD
{
  public class WDExtractor : IWDExtractor
  {
    private readonly ILogger<WDExtractor> _logger;

    public WDExtractor(ILogger<WDExtractor> logger)
    {
      _logger = logger;
    }

    public Task Extract(string filePath, string outputPath = null)
    {
      outputPath ??= Path.GetDirectoryName(filePath);

      var file = File.ReadAllBytes(filePath);

      if (!IsValidWDFile(file))
      {
        throw new NotSupportedException("Unknown file format");
      }

      var dirByte = file.Skip(file.Length - 4).ToArray();
      var dirLn = (int)BitConverter.ToUInt32(dirByte, 0);
      var dirData = file.Skip(file.Length - dirLn).Take(dirLn).ToArray();

      var dir = Decompress(dirData);
      var dirdesc = new Resources.Directory(dir);

      foreach (var desc in dirdesc.Resources.Where(r => !(r is Group)))
      {
        _logger.LogInformation("Extracted {FileInfo}", desc);
        var data = file.Skip((int)desc.Offset).Take((int)desc.Length).ToArray();

        if (data.Length > 0)
        {
          var fileDirectory = Path.GetDirectoryName(desc.Filename);

          if (!System.IO.Directory.Exists(Path.Combine(outputPath, fileDirectory)))
          {
            System.IO.Directory.CreateDirectory(Path.Combine(outputPath, fileDirectory));
          }

          data = desc.DecompressedLength == desc.Length ? data : Decompress(data);
          File.WriteAllBytes(Path.Combine(outputPath, desc.Filename), data);
          if (desc.HasUnknownData)
          {
            File.WriteAllBytes(Path.Combine(outputPath, desc.Filename) + ".unknownData", desc.UnknownData);
          }
          if (desc is TranslatableResource descTranslatable)
          {
            File.WriteAllText(Path.Combine(outputPath, desc.Filename) + ".translationId", descTranslatable.TranslationId);
          }
        }
      }

      return Task.CompletedTask;
    }

    bool IsValidWDFile(byte[] data)
    {
      var header = Decompress(data);
      return header.Take(8).SequenceEqual(new byte[] { 0xff, 0xa1, 0xd0, (byte)'1', (byte)'W', (byte)'D', 0x00, 0x02 });
    }

    byte[] Decompress(byte[] compressedData)
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
