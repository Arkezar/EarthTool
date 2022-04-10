using EarthTool.MSH.Interfaces;
using System.IO;
using System.Text;

namespace EarthTool.MSH.Models
{
  public class MeshFrames : IMeshFrames
  {
    public MeshFrames() { }

    public byte BuildingFrames { get; set; }

    public byte ActionFrames { get; set; }

    public byte MovementFrames { get; set; }

    public byte LoopedFrames { get; set; }

    public byte[] ToByteArray(Encoding encoding)
    {
      using (var output = new MemoryStream())
      {
        using (var writer = new BinaryWriter(output, encoding))
        {
          writer.Write(BuildingFrames);
          writer.Write(ActionFrames);
          writer.Write(MovementFrames);
          writer.Write(LoopedFrames);
        }
        return output.ToArray();
      }
    }
  }
}