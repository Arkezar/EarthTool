using EarthTool.Common.Interfaces;
using System.Drawing;

namespace EarthTool.MSH.Interfaces
{
  public interface ILight : IVector, IBinarySerializable
  {
    Color Color { get; set; }
    bool IsAvailable { get; }
  }
}