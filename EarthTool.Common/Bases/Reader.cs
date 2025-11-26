using EarthTool.Common.Enums;
using EarthTool.Common.Interfaces;
using System.IO;

namespace EarthTool.Common.Bases
{
  public abstract class Reader<T> : IReader<T>
  {
    public abstract FileType InputFileExtension { get; }

    public T Read(string filePath)
      => !File.Exists(filePath) ? default : InternalRead(filePath);

    protected abstract T InternalRead(string filePath);
  }
}
