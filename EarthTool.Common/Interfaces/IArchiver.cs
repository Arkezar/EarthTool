namespace EarthTool.Common.Interfaces
{
  public interface IArchiver
  {
    IArchive OpenArchive(string filePath);
    void Extract(IArchiveFileHeader resource, string outputFilePath);
    void ExtractAll(string outputPath);
  }
}