using System;

namespace EarthTool.Common.Interfaces
{
  public interface IArchiver
  {
    IArchive OpenArchive(string filePath);
    IArchive CreateArchive();
    IArchive CreateArchive(DateTime lastModification);
    IArchive CreateArchive(DateTime lastModification, Guid guid);
    void AddFile(IArchive archive, string filePath, string baseDirectory = null, bool compress = true);
    void SaveArchive(IArchive archive, string outputFilePath);
    void Extract(IArchiveItem resource, string outputPath);
    void ExtractAll(IArchive archive, string outputPath);
  }
}