using EarthTool.Common.Interfaces;
using EarthTool.Common.Models;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace EarthTool.TEX
{
  public class TEXConverter : ITEXConverter
  {
    private readonly ILogger<TEXConverter> _logger;
    private readonly Encoding _encoding;
    private readonly bool _highResolutionOnly;
    private readonly bool _debug;

    public TEXConverter(ILogger<TEXConverter> logger, Encoding encoding)
    {
      _logger = logger;
      _encoding = encoding;
    }

    public TEXConverter(ILogger<TEXConverter> logger, Encoding encoding, IReadOnlyCollection<Option> options) : this(
      logger, encoding)
    {
      _highResolutionOnly = options.Any(o => o.Name == "HighResolutionOnly" && o.GetValue<bool>());
      _debug = options.Any(o => o.Name == "Debug" && o.GetValue<bool>());
    }

    public Task Convert(string filePath, string outputPath = null)
    {
      outputPath ??= Path.GetDirectoryName(filePath);
      var filename = Path.GetFileNameWithoutExtension(filePath);


      using (var stream = File.OpenRead(filePath))
      {
        using (var reader = new BinaryReader(stream))
        {
          try
          {
            var texFile = new TexFile(reader);
            if(texFile.HasHeader && _debug)
            {
              File.WriteAllText(Path.Combine(outputPath, $"{filename}_header.json"), JsonSerializer.Serialize(texFile.Header));
            }
            texFile.Images.SelectMany((img, i) =>
            {
              if (_debug)
              {
                File.WriteAllText(Path.Combine(outputPath, $"{filename}_{i}_header.json"), JsonSerializer.Serialize(img.Header));
              }

              return img.Mipmaps.Take(_highResolutionOnly ? 1 : img.Mipmaps.Count())
                .Select(mm => SaveBitmap(outputPath, filename, i, mm));
            }).ToArray();
          }
          catch (Exception e)
          {
            Console.WriteLine(filePath);
            Console.WriteLine(e);
          }
        }
      }

      return Task.CompletedTask;
    }

    private string SaveBitmap(string workDir, string filename, int i, Image image)
    {
      if (!Directory.Exists(workDir))
      {
        Directory.CreateDirectory(workDir);
      }

      var outputFileName =
        _highResolutionOnly ? $"{filename}_{i}.png" : $"{filename}_{i}_{image.Width}x{image.Height}.png";
      
      var filePath = Path.Combine(workDir, outputFileName);
      image.Save(filePath);
      return filePath;
    }
    
    public IConverter WithOptions(IReadOnlyCollection<Option> options)
    {
      return new TEXConverter(_logger, _encoding, options);
    }
  }
}