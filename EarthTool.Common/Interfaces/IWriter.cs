namespace EarthTool.Common.Interfaces
{
  public interface IWriter<in T>
  {
    string OutputFileExtension { get; }
    
    string Write(T data, string filePath);
  }
}