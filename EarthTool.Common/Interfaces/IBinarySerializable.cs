using System.Text;

namespace EarthTool.Common.Interfaces
{
  public interface IBinarySerializable
  {
    byte[] ToByteArray(Encoding encoding);
  }
}
