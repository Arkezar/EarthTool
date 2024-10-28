using EarthTool.Common.Enums;
using EarthTool.Common.Interfaces;
using EarthTool.TEX.Interfaces;
using System.IO;

namespace EarthTool.TEX
{
  public class TexReader : IReader<ITexFile>
  {
    public FileType InputFileExtension => FileType.TEX;

    public ITexFile Read(string filePath)
    {
      using (var stream = File.OpenRead(filePath))
      {
        using (var reader = new BinaryReader(stream))
        {
          return new TexFile(reader);
        }
      }
    }
  }
}