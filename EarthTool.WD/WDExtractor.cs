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
      _archivizerService.SetArchiveFilePath(filePath);
      _archivizerService.ExtractAll(outputPath);
      return Task.CompletedTask;
    }
  }
}
