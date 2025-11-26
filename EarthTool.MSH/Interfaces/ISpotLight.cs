using EarthTool.Common.Interfaces;

namespace EarthTool.MSH.Interfaces
{
  public interface ISpotLight : ILight, IBinarySerializable
  {
    float Ambience { get; set; }
    int Direction { get; set; }
    float Length { get; set; }
    float Tilt { get; set; }
    float U3 { get; set; }
    float Width { get; set; }
  }
}
