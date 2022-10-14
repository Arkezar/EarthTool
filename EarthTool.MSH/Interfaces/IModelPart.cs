using EarthTool.Common.Interfaces;
using System.Collections.Generic;

namespace EarthTool.MSH.Interfaces
{
  public interface IModelPart : IBinarySerializable
  {
    IAnimations Animations { get; }
    byte BackTrackDepth { get; }
    IEnumerable<IFace> Faces { get; }
    IVector Offset { get; }
    ITextureInfo Texture { get; }
    byte[] UnknownBytes { get; }
    byte PartType { get; }
    double RiseAngle { get; }
    short Empty { get; }
    int AnimationType { get; }
    IEnumerable<IVertex> Vertices { get; }
  }
}