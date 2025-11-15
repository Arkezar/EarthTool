namespace EarthTool.Common.Interfaces
{
  public interface IArchiver
  {
    IArchive OpenArchive(string filePath);
    void Extract(IArchiveItem resource, string outputFilePath);
    void ExtractAll(string outputPath);
  }
}