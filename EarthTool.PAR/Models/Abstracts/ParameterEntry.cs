using System.IO;

namespace EarthTool.PAR.Models
{
  public abstract class ParameterEntry
  {
    protected int GetInteger(BinaryReader data)
    {
      return data.ReadInt32();
    }

    protected string GetString(BinaryReader data)
    {
      return new string(data.ReadChars(data.ReadInt32()));
    }
  }
}