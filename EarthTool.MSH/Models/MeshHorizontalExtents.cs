using EarthTool.MSH.Interfaces;
using System.IO;
using System.Text;

namespace EarthTool.MSH.Models
{
  public class MeshHorizontalExtents : IMeshHorizontalExtents
  {
    public ushort PositiveY { get; set; }

    public ushort NegativeY { get; set; }

    public ushort PositiveX { get; set; }

    public ushort NegativeX { get; set; }

    public byte[] ToByteArray(Encoding encoding)
    {
      using (var output = new MemoryStream())
      {
        using (var writer = new BinaryWriter(output, encoding))
        {
          writer.Write(PositiveY);
          writer.Write(NegativeY);
          writer.Write(PositiveX);
          writer.Write(NegativeX);
        }

        return output.ToArray();
      }
    }
  }
}
