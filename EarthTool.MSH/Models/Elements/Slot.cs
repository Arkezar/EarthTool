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

    public double Direction
    {
      get => Heading / 256.0 * Math.PI * 2.0;
      set
      {
        var turns = value / (Math.PI * 2.0);
        var normalizedTurns = turns - Math.Floor(turns);
        Heading = (byte)(normalizedTurns * 256.0);
      }
    }

    public byte Heading { get; set; }

    public byte FinalParameter { get; set; }

    public bool IsValid
      => Position != null &&
         (Position.X != -128f || Position.Y != 128f || Position.Z != -128f);

    public Slot()
    {
      Position = new Vector(-128f, 128f, -128f);
    }

    public byte[] ToByteArray(Encoding encoding)
    {
      using (var stream = new MemoryStream())
      {
        using (var writer = new BinaryWriter(stream))
        {
          writer.Write((short)(Position.X * 256));
          writer.Write((short)(-Position.Y * 256));
          writer.Write((short)(Position.Z * 256));
          writer.Write(Heading);
          writer.Write(FinalParameter);
        }
        return stream.ToArray();
      }
    }
  }
}
