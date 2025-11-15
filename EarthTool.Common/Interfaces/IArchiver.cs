namespace EarthTool.Common.Interfaces
{
  public interface IArchiver
  {
    IArchive OpenArchive(string filePath);
    IArchive CreateArchive();
    void AddFile(IArchive archive, string filePath, bool compress = true);
    void SaveArchive(IArchive archive, string outputFilePath);
    void Extract(IArchiveItem resource, string outputPath);
    void ExtractAll(IArchive archive, string outputPath);
  }
}