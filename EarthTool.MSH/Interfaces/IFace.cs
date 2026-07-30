using EarthTool.Common.Interfaces;

namespace EarthTool.MSH.Interfaces
{
  public interface IFace : IBinarySerializable
  {
    ushort Flags { get; }
    ushort V1 { get; }
    ushort V2 { get; }
    ushort V3 { get; }
  }
}
