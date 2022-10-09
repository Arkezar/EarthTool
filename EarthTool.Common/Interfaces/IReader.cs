namespace EarthTool.Common.Interfaces
{
  public interface IReader<out T>
  {
    string InputFileExtension { get; }
    
    T Read(string filePath);
  }
}