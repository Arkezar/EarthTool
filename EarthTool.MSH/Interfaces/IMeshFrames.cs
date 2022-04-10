using EarthTool.Common.Interfaces;

namespace EarthTool.MSH.Interfaces
{
  public interface IMeshFrames : IBinarySerializable
  {
    byte ActionFrames { get; }
    byte BuildingFrames { get; }
    byte LoopedFrames { get; }
    byte MovementFrames { get; }
  }
}