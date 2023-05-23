using EarthTool.Common.Interfaces;
using EarthTool.Common.Models;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Unicode;
using System.Threading.Tasks;

namespace EarthTool.LAN
{
  public class LANConverter : ILANConverter
  {
    private static readonly byte[] Identifier = { (byte)'L', (byte)'A', (byte)'N', 0x0, 0x1, 0x0, 0x0, 0x0 };

    private readonly ILogger<LANConverter> _logger;
    private readonly Encoding _encoding;

    public LANConverter(ILogger<LANConverter> logger, Encoding encoding)
    {
      _logger = logger;
      _encoding = encoding;
    }

    public IConverter WithOptions(IReadOnlyCollection<Option> options)
      => this;

    public Task Convert(string filePath, string outputPath = null)
    {
      var inputExtension = Path.GetExtension(filePath);
      return inputExtension.ToUpper().Trim('.') switch
      {
        "LAN" => ConvertToJson(filePath, outputPath),
        "JSON" => ConvertToLan(filePath, outputPath),
        _ => throw new NotSupportedException()
      };
    }

    private async Task ConvertToJson(string filePath, string outputPath)
    {
      using (var fileStream = new FileStream(filePath, FileMode.Open, FileAccess.Read))
      {
        using (var binaryReader = new BinaryReader(fileStream, _encoding))
        {
          var header = binaryReader.ReadBytes(Identifier.Length);
          if (!header.SequenceEqual(Identifier))
          {
            return;
          }

          var dictionary = new Dictionary<string, string>();
          var count = binaryReader.ReadInt32();
          for (var i = 0; i < count; i++)
          {
            var keyLength = binaryReader.ReadInt32();
            var key =  _encoding.GetString(binaryReader.ReadBytes(keyLength));
            var valueLength = binaryReader.ReadInt32();
            var value = _encoding.GetString(binaryReader.ReadBytes(valueLength));
            dictionary.Add(key, value);
          }

          var json = JsonSerializer.Serialize(dictionary, new JsonSerializerOptions { WriteIndented = true, 
            Encoder = JavaScriptEncoder.Create(UnicodeRanges.All)});

          var outputFilePath = Path.Combine(outputPath, Path.ChangeExtension(Path.GetFileName(filePath), "json"));
          await File.WriteAllTextAsync(outputFilePath, json);
        }
      }
    }

    private async Task ConvertToLan(string filePath, string outputPath)
    {
      throw new System.NotImplementedException();
    }
  }
}