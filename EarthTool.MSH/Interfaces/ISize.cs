using EarthTool.Common.Interfaces;

namespace EarthTool.MSH.Interfaces
{
  public interface ISize : IBinarySerializable
  {
    float X1 { get; }
    float Y1 { get; }
    float X2 { get; }
    float Y2 { get; }
  }
}
