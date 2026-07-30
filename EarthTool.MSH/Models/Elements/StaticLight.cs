using EarthTool.MSH.Interfaces;
using System.IO;
using System.Numerics;
using System.Text;

namespace EarthTool.MSH.Models.Elements
{
  public abstract class StaticLight : IStaticLight
  {
    public Vector3 Position { get; set; }

    public Vector3 LightParameters { get; set; }

    public virtual byte[] ToByteArray(Encoding encoding)
    {
      using (var stream = new MemoryStream())
      {
        using (var writer = new BinaryWriter(stream))
        {
          writer.Write(Position.X);
          writer.Write(-Position.Y);
          writer.Write(Position.Z);
          writer.Write(LightParameters.X);
          writer.Write(LightParameters.Y);
          writer.Write(LightParameters.Z);
        }
        return stream.ToArray();
      }
    }
  }
}
