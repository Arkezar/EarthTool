using EarthTool.MSH.Interfaces;
using System.IO;
using System.Numerics;
using System.Text;

namespace EarthTool.MSH.Models.Elements
{
  public class RotationFrame : IRotationFrame
  {
    public Matrix4x4 TransformationMatrix
    {
      get; set;
    }

    public byte[] ToByteArray(Encoding encoding)
    {
      using (var stream = new MemoryStream())
      {
        using (var writer = new BinaryWriter(stream))
        {
          writer.Write(TransformationMatrix.M11);
          writer.Write(TransformationMatrix.M21);
          writer.Write(TransformationMatrix.M31);
          writer.Write(TransformationMatrix.M41);

          writer.Write(TransformationMatrix.M12);
          writer.Write(TransformationMatrix.M22);
          writer.Write(TransformationMatrix.M32);
          writer.Write(TransformationMatrix.M42);

          writer.Write(TransformationMatrix.M13);
          writer.Write(TransformationMatrix.M23);
          writer.Write(TransformationMatrix.M33);
          writer.Write(TransformationMatrix.M43);

          writer.Write(TransformationMatrix.M14);
          writer.Write(TransformationMatrix.M24);
          writer.Write(TransformationMatrix.M34);
          writer.Write(TransformationMatrix.M44);
        }
        return stream.ToArray();
      }
    }
  }
}
