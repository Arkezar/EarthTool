using EarthTool.Common.Interfaces;

namespace EarthTool.MSH.Interfaces
{
  public interface IFace : IBinarySerializable
  {
    short UNKNOWN { get; }
    short V1 { get; }
    short V2 { get; }
    short V3 { get; }
  }
}
