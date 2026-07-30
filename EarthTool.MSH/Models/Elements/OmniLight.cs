using EarthTool.MSH.Interfaces;
using System.IO;
using System.Text;

namespace EarthTool.MSH.Models.Elements
{
  public class OmniLight : StaticLight, IOmniLight
  {
    public float FinalParameter { get; set; }

    public override byte[] ToByteArray(Encoding encoding)
    {
      using (var stream = new MemoryStream())
      {
        using (var writer = new BinaryWriter(stream))
        {
          writer.Write(base.ToByteArray(encoding));
          writer.Write(FinalParameter);
        }
        return stream.ToArray();
      }
    }
  }
}
