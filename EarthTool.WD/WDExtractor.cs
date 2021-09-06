using EarthTool.Common.Interfaces;
using Microsoft.Extensions.Logging;
using System.IO;
using System.Threading.Tasks;

namespace EarthTool.WD
{
  public class WDExtractor : IWDExtractor
  {
    private readonly ILogger<WDExtractor> _logger;
    private readonly IArchivizer _archivizerService;

    public WDExtractor(ILogger<WDExtractor> logger, IArchivizer archivizerService)
    {
      _logger = logger;
      _archivizerService = archivizerService;
    }

    public Task Extract(string filePath, string outputPath = null)
    {
      outputPath ??= Path.GetDirectoryName(filePath);

      var valid = _archivizerService.VerifyFile(filePath);
      if (valid)
      {
        var descriptor = _archivizerService.GetArchiveDescriptor(filePath);
        var files = _archivizerService.GetResourceWithData(filePath, descriptor.Resources);

        foreach (var file in files)
        {
          var outputFilePath = Path.Combine(outputPath, file.Filename);
          if (!Directory.Exists(Path.GetDirectoryName(outputFilePath)))
          {
            Directory.CreateDirectory(Path.GetDirectoryName(outputFilePath));
          }

          _logger.LogInformation("Extracted {File}", file.Filename);
          File.WriteAllBytes(outputFilePath, file.Data);
        }
      }

      return Task.CompletedTask;
    }
  }
}
