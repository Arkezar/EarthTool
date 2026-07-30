using EarthTool.Common.Interfaces;

namespace EarthTool.MSH.Interfaces
{
  public interface IOmniLight : IStaticLight, IBinarySerializable
  {
    float FinalParameter { get; set; }
  }
}
