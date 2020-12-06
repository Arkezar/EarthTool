namespace EarthTool.Common.Interfaces
{
  public interface IExtractor
  {
    int Extract(string filePath, string outputPath = null);
  }
}
