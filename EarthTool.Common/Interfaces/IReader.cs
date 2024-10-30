using EarthTool.Common.Enums;

namespace EarthTool.Common.Interfaces
{
  public interface IReader<out T>
  {
    FileType InputFileExtension { get; }

    T Read(string filePath);
  }
}