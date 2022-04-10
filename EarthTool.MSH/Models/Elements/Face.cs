using EarthTool.MSH.Interfaces;
using System.IO;
using System.Text;

namespace EarthTool.MSH.Models.Elements
{
  public class Face : IFace
  {
    public short V1 { get; set; }

    public short V2 { get; set; }

    public short V3 { get; set; }

    public short UNKNOWN { get; set; }

    public byte[] ToByteArray(Encoding encoding)
    {
      using (var stream = new MemoryStream())
      {
        using (var writer = new BinaryWriter(stream))
        {
          writer.Write(V1);
          writer.Write(V2);
          writer.Write(V3);
          writer.Write(UNKNOWN);
        }
        return stream.ToArray();
      }
    }
  }
}
