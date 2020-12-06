namespace EarthTool.Common.Interfaces
{
  public interface IConverter
  {
    int Convert(string filePath, string outputPath = null);
  }
}
