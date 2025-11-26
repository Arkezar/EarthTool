using EarthTool.Common.Bases;
using EarthTool.Common.Enums;
using EarthTool.PAR.Models;
using System.IO;
using System.Text;

namespace EarthTool.PAR.Services
{
  public class ParameterWriter : Writer<ParFile>
  {
    private readonly Encoding _encoding;

    public ParameterWriter(Encoding encoding)
    {
      _encoding = encoding;
    }

    public override FileType OutputFileExtension => FileType.PAR;

    protected override string InternalWrite(ParFile data, string filePath)
    {
      using (FileStream stream = File.Create(filePath))
      {
        using (BinaryWriter writer = new BinaryWriter(stream, _encoding))
        {
          writer.Write(data.ToByteArray(_encoding));
        }

        return filePath;
      }
    }
  }
}
