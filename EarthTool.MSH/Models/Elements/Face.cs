using EarthTool.MSH.Interfaces;
using System.IO;
using System.Text;

namespace EarthTool.MSH.Models.Elements
{
  public class Face : IFace
  {
    public ushort V1 { get; set; }

    public ushort V2 { get; set; }

    public ushort V3 { get; set; }

    public ushort Flags { get; set; }

    public byte[] ToByteArray(Encoding encoding)
    {
      using (var stream = new MemoryStream())
      {
        using (var writer = new BinaryWriter(stream))
        {
          writer.Write(V1);
          writer.Write(V2);
          writer.Write(V3);
          writer.Write(Flags);
        }
        return stream.ToArray();
      }
    }
  }
}
