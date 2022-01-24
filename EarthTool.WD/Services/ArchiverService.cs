using EarthTool.Common.Extensions;
using EarthTool.Common.Interfaces;
using EarthTool.WD.Resources;
using Microsoft.Extensions.Logging;
using System;
using System.IO;

namespace EarthTool.WD.Services
{
  public class ArchiverService : IArchiver
  {
    private readonly ILogger<ArchiverService> _logger;
    private readonly IArchiveFactory _archiveFactory;

    private IArchive _archive;

    public ArchiverService(ILogger<ArchiverService> logger, IArchiveFactory archiveFactory)
    {
      _logger = logger;
      _archiveFactory = archiveFactory;
    }

    public IArchive OpenArchive(string filePath)
      => _archive = _archiveFactory.OpenArchive(filePath);

    public void Extract(IArchiveFileHeader resource, string outputFilePath)
    {
      outputFilePath = outputFilePath.Replace('\\', Path.DirectorySeparatorChar);

      if (!Directory.Exists(Path.GetDirectoryName(outputFilePath)))
      {
        Directory.CreateDirectory(Path.GetDirectoryName(outputFilePath));
      }

      File.WriteAllBytes(outputFilePath, _archive.ExtractResource(resource));
    }

    public void ExtractAll(string outputPath)
    {
      foreach (var resource in _archive.CentralDirectory.FileHeaders)
      {
        var outputFilePath = Path.Combine(outputPath, resource.FileName).Replace('\\', Path.DirectorySeparatorChar);

        var outputFolderPath = Path.GetDirectoryName(outputFilePath);

        if (!Directory.Exists(outputFolderPath))
        {
          Directory.CreateDirectory(outputFolderPath);
        }

        File.WriteAllBytes(outputFilePath, _archive.ExtractResource(resource));
        _logger.LogInformation("Extracted file {FileName}", resource.FileName);
      }
    }
  }
}
