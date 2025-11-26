using EarthTool.Common.Interfaces;

namespace EarthTool.MSH.Interfaces
{
  public interface IModelTemplate : IBinarySerializable
  {
    short Flag { get; }
    bool[,] Matrix { get; }
  }
}
