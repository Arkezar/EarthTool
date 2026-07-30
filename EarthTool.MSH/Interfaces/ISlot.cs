using EarthTool.Common.Interfaces;

namespace EarthTool.MSH.Interfaces
{
  public interface ISlot : IBinarySerializable
  {
    double Direction { get; }
    byte FinalParameter { get; }
    byte Heading { get; }
    int Id { get; }
    bool IsValid { get; }
    IVector Position { get; }
  }
}
