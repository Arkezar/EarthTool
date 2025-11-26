using EarthTool.Common.Interfaces;
using System.IO;

namespace EarthTool.MSH.Interfaces
{
  public interface IMeshBoundries : IBinarySerializable
  {
    short MaxX { get; }
    short MaxY { get; }
    short MinX { get; }
    short MinY { get; }
  }
}
