using EarthTool.Common.Bases;
using EarthTool.Common.Enums;
using EarthTool.MSH.Interfaces;
using System.IO;
using System.Text;

namespace EarthTool.MSH.Services
{
  public class EarthMeshWriter : Writer<IMesh>
  {
    private readonly Encoding _encoding;

    public EarthMeshWriter(Encoding encoding)
    {
      _encoding = encoding;
    }

    public override FileType OutputFileExtension => FileType.MSH;

    protected override string InternalWrite(IMesh data, string filePath)
    {
      using (var stream = File.Create(filePath))
      {
        using (var writer = new BinaryWriter(stream, _encoding))
        {
          writer.Write(data.ToByteArray(_encoding));
        }

        return filePath;
      }
    }
  }
}
