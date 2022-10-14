using EarthTool.MSH.Interfaces;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace EarthTool.MSH.Models.Collections
{
  public class Animations : IAnimations
  {
    public IEnumerable<IVector> ScaleFrames { get; set; }
    public IEnumerable<IVector> MovementFrames { get; set; }
    public IEnumerable<IRotationFrame> RotationFrames { get; set; }

    public Animations()
    {
      ScaleFrames = Enumerable.Empty<IVector>();
      MovementFrames = Enumerable.Empty<IVector>();
      RotationFrames = Enumerable.Empty<IRotationFrame>();
    }

    public byte[] ToByteArray(Encoding encoding)
    {
      using (var stream = new MemoryStream())
      {
        using (var writer = new BinaryWriter(stream))
        {
          writer.Write(ScaleFrames.Count());
          writer.Write(ScaleFrames.SelectMany(x => x.ToByteArray(encoding)).ToArray());
          writer.Write(MovementFrames.Count());
          writer.Write(MovementFrames.SelectMany(x => x.ToByteArray(encoding)).ToArray());
          writer.Write(RotationFrames.Count());
          writer.Write(RotationFrames.SelectMany(x => x.ToByteArray(encoding)).ToArray());
        }
        return stream.ToArray();
      }
    }
  }
}
