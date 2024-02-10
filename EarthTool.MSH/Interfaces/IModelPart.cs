using EarthTool.Common.Interfaces;
using EarthTool.MSH.Enums;
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
    byte UnknownFlag { get; }
    byte UnknownByte1 { get; }
    byte UnknownByte2 { get; }
    byte UnknownByte3 { get; }
    PartType PartType { get; }
    double RiseAngle { get; }
    short Empty { get; }
    AnimationType AnimationType { get; }
    IEnumerable<IVertex> Vertices { get; }
  }
}