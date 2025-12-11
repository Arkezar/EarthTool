using EarthTool.Common.Enums;
using EarthTool.Common.Interfaces;
using EarthTool.TEX.Interfaces;
using System.IO;

namespace EarthTool.TEX
{
  public class TexReader : IReader<ITexFile>
  {
    private readonly IEarthInfoFactory _earthInfoFactory;
    public FileType InputFileExtension => FileType.TEX;

    public TexReader(IEarthInfoFactory earthInfoFactory)
    {
      _earthInfoFactory = earthInfoFactory;
    }
    
    public ITexFile Read(string filePath)
    {
      using (var stream = File.OpenRead(filePath))
      {
        using (var reader = new BinaryReader(stream))
        {
          var fileInfo = _earthInfoFactory.Get(stream);
          return new TexFile(reader, fileInfo);
        }
      }
    }
  }
}
