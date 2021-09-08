using System.Collections.Generic;

namespace EarthTool.Common.Interfaces
{
  public interface IArchivizer
  {
    string ArchiveFilePath { get; }
    IArchive GetArchiveDescriptor();
    IArchiveHeader GetArchiveHeader();
    bool VerifyFile();
    void Extract(string outputFilePath, IArchiveResource resource);
    void ExtractAll(string outputPath);
    void SetArchiveFilePath(string filePath);
  }
}