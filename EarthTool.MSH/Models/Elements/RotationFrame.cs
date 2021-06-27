using EarthTool.Common.Extensions;
using System;
using System.IO;
using System.Numerics;

namespace EarthTool.MSH.Models.Elements
{
  public class RotationFrame
  {
    public Matrix4x4 TransformationMatrix
    {
      get;
    }

    public RotationFrame(Stream stream)
    {
      TransformationMatrix = new Matrix4x4()
      {
        M11 = BitConverter.ToSingle(stream.ReadBytes(4)),
        M12 = -BitConverter.ToSingle(stream.ReadBytes(4)),
        M13 = -BitConverter.ToSingle(stream.ReadBytes(4)),
        M14 = BitConverter.ToSingle(stream.ReadBytes(4)),
        M21 = -BitConverter.ToSingle(stream.ReadBytes(4)),
        M22 = BitConverter.ToSingle(stream.ReadBytes(4)),
        M23 = -BitConverter.ToSingle(stream.ReadBytes(4)),
        M24 = BitConverter.ToSingle(stream.ReadBytes(4)),
        M31 = -BitConverter.ToSingle(stream.ReadBytes(4)),
        M32 = -BitConverter.ToSingle(stream.ReadBytes(4)),
        M33 = BitConverter.ToSingle(stream.ReadBytes(4)),
        M34 = BitConverter.ToSingle(stream.ReadBytes(4)),
        M41 = BitConverter.ToSingle(stream.ReadBytes(4)),
        M42 = BitConverter.ToSingle(stream.ReadBytes(4)),
        M43 = BitConverter.ToSingle(stream.ReadBytes(4)),
        M44 = BitConverter.ToSingle(stream.ReadBytes(4))
      };
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
  }
}
