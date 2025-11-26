using EarthTool.Common.Interfaces;

namespace EarthTool.MSH.Interfaces
{
  public interface ITextureInfo : IBinarySerializable
  {
    string FileName { get; }
  }
}
