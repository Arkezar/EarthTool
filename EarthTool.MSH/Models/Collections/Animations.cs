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
    public IEnumerable<IVector> TranslationFrames { get; set; }
    public IEnumerable<IRotationFrame> RotationFrames { get; set; }

    public Animations()
    {
      ScaleFrames = Enumerable.Empty<IVector>();
      TranslationFrames = Enumerable.Empty<IVector>();
      RotationFrames = Enumerable.Empty<IRotationFrame>();
    }

    public byte[] ToByteArray(Encoding encoding)
    {
      var scaleFrames = ScaleFrames.ToArray();
      var translationFrames = TranslationFrames.ToArray();
      var rotationFrames = RotationFrames.ToArray();
      using (var stream = new MemoryStream())
      {
        using (var writer = new BinaryWriter(stream))
        {
          writer.Write(scaleFrames.Length);
          foreach (var frame in scaleFrames)
          {
            writer.Write(frame.X);
            writer.Write(frame.Y);
            writer.Write(frame.Z);
          }
          writer.Write(translationFrames.Length);
          writer.Write(translationFrames.SelectMany(x => x.ToByteArray(encoding)).ToArray());
          writer.Write(rotationFrames.Length);
          writer.Write(rotationFrames.SelectMany(x => x.ToByteArray(encoding)).ToArray());
        }
        return stream.ToArray();
      }
    }
  }
}
