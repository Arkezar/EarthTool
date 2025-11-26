using EarthTool.MSH.Interfaces;
using System.IO;
using System.Text;

namespace EarthTool.MSH.Models
{
  public class MeshBoundries : IMeshBoundries
  {
    public short MaxY { get; set; }

    public short MinY { get; set; }

    public short MaxX { get; set; }

    public short MinX { get; set; }

    public byte[] ToByteArray(Encoding encoding)
    {
      using (var output = new MemoryStream())
      {
        using (var writer = new BinaryWriter(output, encoding))
        {
          writer.Write(MaxY);
          writer.Write(MinY);
          writer.Write(MaxX);
          writer.Write(MinX);
        }
        return output.ToArray();
      }
    }
  }
}
