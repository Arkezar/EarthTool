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

    public byte Flag { get; set; }

    public bool IsValid
      => Flag == 128;

    public Slot()
    {
      var val = short.MinValue / (float)byte.MaxValue;
      Position = new Vector(val, val, val);
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
          writer.Write(Flag);
        }
        return stream.ToArray();
      }
    }
  }
}
