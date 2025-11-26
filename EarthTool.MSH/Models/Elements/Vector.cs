using EarthTool.MSH.Interfaces;
using System.IO;
using System.Numerics;
using System.Text;

namespace EarthTool.MSH.Models.Elements
{
  public class Vector : IVector
  {
    public Vector()
    {
      Value = new Vector3(0f, -0f, 0f);
    }

    public Vector(float x, float y, float z)
    {
      Value = new Vector3(x, y, z);
    }

    public float X => Value.X;

    public float Y => Value.Y;

    public float Z => Value.Z;

    public Vector3 Value { get; set; }

    public bool Equals(IVector other)
    {
      return X == other.X && Y == other.Y && Z == other.Z;
    }

    public virtual byte[] ToByteArray(Encoding encoding)
    {
      using (var stream = new MemoryStream())
      {
        using (var writer = new BinaryWriter(stream))
        {
          writer.Write(X);
          writer.Write(-Y);
          writer.Write(Z);
        }
        return stream.ToArray();
      }
    }
  }
}
