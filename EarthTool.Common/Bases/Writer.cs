using EarthTool.Common.Enums;
using EarthTool.Common.Interfaces;
using System.IO;

namespace EarthTool.Common.Bases
{
  public abstract class Writer<T> : IWriter<T>
  {
    public abstract FileType OutputFileExtension { get; }
    
    public string Write(T data, string filePath)
    {
      var outputFolder = Path.GetDirectoryName(filePath);

      if (!Directory.Exists(outputFolder))
      {
        Directory.CreateDirectory(outputFolder);
      }

      return InternalWrite(data, filePath);
    }

    protected abstract string InternalWrite(T data, string filePath);
  }
}