namespace EarthTool.Common.Interfaces
{
  public interface IWriter<in T>
  {
    string OutputFileExtension { get; }
    
    void Write(T data, string filePath);
  }
}