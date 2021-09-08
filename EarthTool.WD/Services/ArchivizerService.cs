using EarthTool.Common.Extensions;
using EarthTool.Common.Interfaces;
using EarthTool.WD.Resources;
using Microsoft.Extensions.Logging;
using System;
using System.IO;

namespace EarthTool.WD.Services
{
  public class ArchivizerService : IArchivizer
  {
    private readonly ILogger<ArchivizerService> _logger;
    private readonly IEncryption _encryption;

    public string ArchiveFilePath
    {
      get;
      private set;
    }

    public ArchivizerService(ILogger<ArchivizerService> logger, IEncryption encryption)
    {
      _logger = logger;
      _encryption = encryption;
    }

    public void SetArchiveFilePath(string filePath)
    {
      ArchiveFilePath = filePath;
    }

    public bool VerifyFile()
    {
      return GetArchiveHeader().IsValid();
    }

    public IArchiveHeader GetArchiveHeader()
    {
      using (var stream = new FileStream(ArchiveFilePath, FileMode.Open))
      {
        return new ArchiveHeader(_encryption.Decompress(stream));
      }
    }

    public IArchive GetArchiveDescriptor()
    {
      using (var stream = new FileStream(ArchiveFilePath, FileMode.Open))
      {
        stream.Seek(-4, SeekOrigin.End);
        var descriptorLength = BitConverter.ToInt32(stream.ReadBytes(4));
        stream.Seek(-descriptorLength, SeekOrigin.End);

        return new Archive(_encryption.Decompress(stream));
      }
    }

    public void Extract(string outputFilePath, IArchiveResource resource)
    {
      if (!VerifyFile())
      {
        return;
      }

      if (!Directory.Exists(Path.GetDirectoryName(outputFilePath)))
      {
        Directory.CreateDirectory(Path.GetDirectoryName(outputFilePath));
      }

      File.WriteAllBytes(outputFilePath, GetResourceData(resource));
    }

    public void ExtractAll(string outputPath)
    {
      if (!VerifyFile())
      {
        return;
      }

      foreach (var resource in GetArchiveDescriptor().Resources)
      {
        var outputFilePath = Path.Combine(outputPath, resource.Filename);
        var outputFolderPath = Path.GetDirectoryName(outputFilePath);

        if (!Directory.Exists(outputFolderPath))
        {
          Directory.CreateDirectory(outputFolderPath);
        }

        File.WriteAllBytes(outputFilePath, GetResourceData(resource));
      }
    }

    private byte[] GetResourceData(IArchiveResource resource)
    {
      using (var stream = new FileStream(ArchiveFilePath, FileMode.Open))
      {
        var data = resource.GetData(stream);
        return resource.Flags.HasFlag(Common.Enums.FileFlags.Compressed) ? _encryption.Decompress(data) : data;
      }
    }
  }
}
