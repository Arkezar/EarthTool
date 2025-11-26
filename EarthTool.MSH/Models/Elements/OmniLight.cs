using EarthTool.MSH.Interfaces;
using System.IO;
using System.Text;

namespace EarthTool.MSH.Models.Elements
{
  public class OmniLight : Light, IOmniLight
  {
    public float Radius { get; set; }

    public override byte[] ToByteArray(Encoding encoding)
    {
      using (var stream = new MemoryStream())
      {
        using (var writer = new BinaryWriter(stream))
        {
          writer.Write(base.ToByteArray(encoding));
          writer.Write(Radius);
        }
        return stream.ToArray();
      }
    }
  }
}
