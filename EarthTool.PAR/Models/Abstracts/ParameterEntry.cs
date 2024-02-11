using EarthTool.Common.Interfaces;
using System.IO;
using System.Text;

namespace EarthTool.PAR.Models
{
  public abstract class ParameterEntry : IBinarySerializable
  {
    protected int GetInteger(BinaryReader data)
    {
      return data.ReadInt32();
    }

    protected string GetString(BinaryReader data)
    {
      return new string(data.ReadChars(data.ReadInt32()));
    }

    public abstract byte[] ToByteArray(Encoding encoding);
  }
}