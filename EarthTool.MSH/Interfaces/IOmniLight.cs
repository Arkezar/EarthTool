using EarthTool.Common.Interfaces;

namespace EarthTool.MSH.Interfaces
{
  public interface IOmniLight : ILight, IBinarySerializable
  {
    float Radius { get; set; }
  }
}