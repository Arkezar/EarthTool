using EarthTool.Common.Enums;

namespace EarthTool.Common.Interfaces
{
  public interface IWriter<in T>
  {
    FileType OutputFileExtension { get; }

    string Write(T data, string filePath);
  }
}
