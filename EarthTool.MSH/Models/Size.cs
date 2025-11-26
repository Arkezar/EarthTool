using EarthTool.MSH.Interfaces;
using System.IO;
using System.Linq;
using System.Text;

namespace EarthTool.MSH.Models
{
  public class Size : ISize
  {
    public float X1 { get; set; }
    public float Y1 { get; set; }
    public float X2 { get; set; }
    public float Y2 { get; set; }

    public byte[] ToByteArray(Encoding encoding)
    {
      using (var output = new MemoryStream())
      {
        using (var bw = new BinaryWriter(output, encoding))
        {
          bw.Write(X1);
          bw.Write(X2);
          bw.Write(Y1);
          bw.Write(Y2);
        }

        return output.ToArray().ToArray();
      }
    }
  }
}
