namespace EarthTool.MSH.Interfaces
{
  public interface IVertex
  {
    IVector Normal { get; }
    IVector Position { get; }
    IUVMap UVMap { get; }
    short UnknownValue1 { get; }
    short UnknownValue2 { get; }
  }
}