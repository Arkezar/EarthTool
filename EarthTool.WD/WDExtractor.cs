using EarthTool.Common.Interfaces;
using EarthTool.Common.Validation;
using Microsoft.Extensions.Logging;
using System;
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
      var validatedPath = PathValidator.ValidateFileExists(filePath);

      outputPath ??= Path.GetDirectoryName(validatedPath)
        ?? throw new InvalidOperationException($"Cannot determine output path for: {validatedPath}");

      try
      {
        using var archive = _archiverService.OpenArchive(validatedPath);
        _logger.LogInformation("Extracting archive {FilePath} to {OutputPath}", validatedPath, outputPath);
        _archiverService.ExtractAll(archive, outputPath);
        _logger.LogInformation("Successfully extracted {ItemCount} items from {FilePath}",
          archive.Items.Count, validatedPath);
      }
      catch (Exception ex)
      {
        _logger.LogError(ex, "Failed to extract archive {FilePath}", validatedPath);
        throw;
      }

      return Task.CompletedTask;
    }
  }
}