using EarthTool.MSH.Interfaces;
using System.IO;
using System.Text;

namespace EarthTool.MSH.Models.Elements
{
  public class TextureInfo : ITextureInfo
  {
    public string FileName { get; set; }

    public byte[] ToByteArray(Encoding encoding)
    {
      using (var stream = new MemoryStream())
      {
        using (var writer = new BinaryWriter(stream))
        {
          writer.Write(FileName.Length);
          writer.Write(encoding.GetBytes(FileName));
        }
        return stream.ToArray();
      }
    }
  }
}
