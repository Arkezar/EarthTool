using EarthTool.MSH.Interfaces;
using System.IO;
using System.Text;

namespace EarthTool.MSH.Models.Elements
{
  public class SpotLight : Light, ISpotLight
  {
    public float Length { get; set; }

    public int Direction { get; set; }

    public float Width { get; set; }

    public float U3 { get; set; }

    public float Tilt { get; set; }

    public float Ambience { get; set; }

    public override byte[] ToByteArray(Encoding encoding)
    {
      using (var stream = new MemoryStream())
      {
        using (var writer = new BinaryWriter(stream))
        {
          writer.Write(base.ToByteArray(encoding));
          writer.Write(Length);
          writer.Write(Direction);
          writer.Write(Width);
          writer.Write(U3);
          writer.Write(Tilt);
          writer.Write(Ambience);
        }
        return stream.ToArray();
      }
    }
  }
}
