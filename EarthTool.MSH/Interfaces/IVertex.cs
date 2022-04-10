namespace EarthTool.MSH.Interfaces
{
  public interface IVertex
  {
    IVector Normal { get; }
    IVector Position { get; }
    float U { get; }
    short UnknownValue1 { get; }
    short UnknownValue2 { get; }
    float V { get; }
  }
}