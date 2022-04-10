using EarthTool.MSH.Interfaces;
using System;
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

    public Quaternion Quaternion
    {
      get
      {
        // Adapted from: http://www.euclideanspace.com/maths/geometry/rotations/conversions/matrixToQuaternion/index.htm
        Quaternion q = new Quaternion();
        q.W = (float)Math.Sqrt(Math.Max(0, 1 + TransformationMatrix.M11 + TransformationMatrix.M22 + TransformationMatrix.M33)) / 2.0f;
        q.X = (float)Math.Sqrt(Math.Max(0, 1 + TransformationMatrix.M11 - TransformationMatrix.M22 - TransformationMatrix.M33)) / 2.0f;
        q.Y = (float)Math.Sqrt(Math.Max(0, 1 - TransformationMatrix.M11 + TransformationMatrix.M22 - TransformationMatrix.M33)) / 2.0f;
        q.Z = (float)Math.Sqrt(Math.Max(0, 1 - TransformationMatrix.M11 - TransformationMatrix.M22 + TransformationMatrix.M33)) / 2.0f;
        q.X *= Math.Sign(q.X * (TransformationMatrix.M31 - TransformationMatrix.M23));
        q.Y *= Math.Sign(q.Y * (TransformationMatrix.M13 - TransformationMatrix.M31));
        q.Z *= Math.Sign(q.Z * (TransformationMatrix.M21 - TransformationMatrix.M12));
        return q;
      }
    }

    public void FromStream(Stream stream)
    {
      using (var reader = new BinaryReader(stream, Encoding.GetEncoding("ISO-8859-2"), true))
      {
        TransformationMatrix = new Matrix4x4()
        {
          M11 = reader.ReadSingle(),
          M12 = -reader.ReadSingle(),
          M13 = -reader.ReadSingle(),
          M14 = reader.ReadSingle(),
          M21 = -reader.ReadSingle(),
          M22 = reader.ReadSingle(),
          M23 = -reader.ReadSingle(),
          M24 = reader.ReadSingle(),
          M31 = -reader.ReadSingle(),
          M32 = -reader.ReadSingle(),
          M33 = reader.ReadSingle(),
          M34 = reader.ReadSingle(),
          M41 = reader.ReadSingle(),
          M42 = reader.ReadSingle(),
          M43 = reader.ReadSingle(),
          M44 = reader.ReadSingle()
        };
      }
    }

    public byte[] ToByteArray(Encoding encoding)
    {
      using (var stream = new MemoryStream())
      {
        using (var writer = new BinaryWriter(stream))
        {
          writer.Write(TransformationMatrix.M11);
          writer.Write(-TransformationMatrix.M12);
          writer.Write(-TransformationMatrix.M13);
          writer.Write(TransformationMatrix.M14);
          writer.Write(-TransformationMatrix.M21);
          writer.Write(TransformationMatrix.M22);
          writer.Write(-TransformationMatrix.M23);
          writer.Write(TransformationMatrix.M24);
          writer.Write(-TransformationMatrix.M31);
          writer.Write(-TransformationMatrix.M32);
          writer.Write(TransformationMatrix.M33);
          writer.Write(TransformationMatrix.M34);
          writer.Write(TransformationMatrix.M41);
          writer.Write(TransformationMatrix.M42);
          writer.Write(TransformationMatrix.M43);
          writer.Write(TransformationMatrix.M44);
        }
        return stream.ToArray();
      }
    }
  }
}
