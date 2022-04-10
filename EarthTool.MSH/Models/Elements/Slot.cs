using EarthTool.MSH.Interfaces;
using System;
using System.IO;
using System.Text;

namespace EarthTool.MSH.Models.Elements
{
  public class Slot : ISlot
  {
    public int Id { get; set; }

    public IVector Position { get; set; }

    public double Direction { get; set; }

    public int Flag { get; set; }

    public bool IsValid
      => Position.X != -128 && Position.Y != -128 && Position.Z != -128;

    public Slot() { }

    public Slot(int id)
    {
      Id = id;
    }

    public Slot(Stream stream, int id) : this(id)
    {
      FromStream(stream);
    }

    public void FromStream(Stream stream)
    {
      using (var reader = new BinaryReader(stream, Encoding.GetEncoding("ISO-8859-2"), true))
      {
        var x = reader.ReadInt16() / 255f;
        var y = -reader.ReadInt16() / 255f;
        var z = reader.ReadInt16() / 255f;
        Position = new Vector(x, y, z);
        Direction = reader.ReadByte() / 255.0 * Math.PI * 2.0;
        Flag = stream.ReadByte();
      }
    }

    public byte[] ToByteArray(Encoding encoding)
    {
      using (var stream = new MemoryStream())
      {
        using (var writer = new BinaryWriter(stream))
        {
          writer.Write((short)(Position.X * 255));
          writer.Write((short)(-Position.Y * 255));
          writer.Write((short)(Position.Z * 255));
          writer.Write((byte)(Direction / 2 / Math.PI * 255));
          writer.Write((byte)Flag);
        }
        return stream.ToArray();
      }
    }
  }
}
