using EarthTool.Common.Interfaces;
using Microsoft.Extensions.Logging;
using System.IO;
using System.Threading.Tasks;

namespace EarthTool.WD
{
  public class WDExtractor : IWDExtractor
  {
    private readonly ILogger<WDExtractor> _logger;
    private readonly IArchiver _archiverService;

    public WDExtractor(ILogger<WDExtractor> logger, IArchiver archiverService)
    {
      _logger = logger;
      _archiverService = archiverService;
    }

    public Task Extract(string filePath, string outputPath = null)
    {
      outputPath ??= Path.GetDirectoryName(filePath);
      var archive = _archiverService.OpenArchive(filePath);
      _archiverService.ExtractAll(archive, outputPath);
      return Task.CompletedTask;
    }
  }
}
