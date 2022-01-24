using EarthTool.Common.Interfaces;
using Microsoft.Extensions.Logging;
using System.IO;
using System.Threading.Tasks;

namespace EarthTool.WD
{
  public class WDExtractor : IWDExtractor
  {
    private readonly ILogger<WDExtractor> _logger;
    private readonly IArchiver _archivizerService;

    public WDExtractor(ILogger<WDExtractor> logger, IArchiver archivizerService)
    {
      _logger = logger;
      _archivizerService = archivizerService;
    }

    public Task Extract(string filePath, string outputPath = null)
    {
      outputPath ??= Path.GetDirectoryName(filePath);
      var archive = _archivizerService.OpenArchive(filePath);
      _archivizerService.ExtractAll(outputPath);
      return Task.CompletedTask;
    }
  }
}
