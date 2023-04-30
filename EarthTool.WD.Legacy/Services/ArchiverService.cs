using EarthTool.Common.Interfaces;
using Microsoft.Extensions.Logging;
using System.Text;

namespace EarthTool.WD.Legacy.Services
{
  public class ArchiverService : IArchiver
  {
    private readonly ILogger<ArchiverService> _logger;
    private readonly IArchiveFactory _archiveFactory;
    private readonly Encoding _encoding;
    private IArchive _archive;

    public ArchiverService(ILogger<ArchiverService> logger, IArchiveFactory archiveFactory, Encoding encoding)
    {
      _logger = logger;
      _archiveFactory = archiveFactory;
      _encoding = encoding;
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

      var fileHeader = resource.ToEarthInfo();
      var data = _archive.ExtractResource(resource);

      var outputData = fileHeader?.ToByteArray(_encoding) ?? new byte[0];
      outputData = outputData.Concat(data).ToArray();

      File.WriteAllBytes(outputFilePath, outputData);
    }

    public void ExtractAll(string outputPath)
    {
      foreach (var resource in _archive.CentralDirectory.FileHeaders)
      {
        var outputFilePath = Path.Combine(outputPath, resource.FileName).Replace('\\', Path.DirectorySeparatorChar);

        Extract(resource, outputFilePath);

        _logger.LogInformation("Extracted file {FileName}", resource.FileName);
      }
    }
  }
}
