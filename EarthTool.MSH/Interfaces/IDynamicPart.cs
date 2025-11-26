using EarthTool.Common.Interfaces;
using System.Collections.Generic;
using System.Drawing;
using System.Numerics;

namespace EarthTool.MSH.Interfaces
{
  public interface IDynamicPart : IBinarySerializable
  {
    LightType LightType { get; }
    int SpriteStartIndex { get; }
    int SpriteAnimationLength { get; }
    int SpriteSheetVertical { get; }
    int SpriteSheetHorizontal { get; }
    int Framerate { get; }
    float TextureSplitRatioVertical { get; }
    float TextureSplitRatioHorizontal { get; }
    ISize Size1 { get; }
    ISize Size2 { get; }
    float SizeZ { get; }
    float Radius { get; }
    bool Additive { get; }
    Color LightColor { get; }
    Color Color { get; }
    float ColorIntensity { get; }
    int AlphaInt { get; }
    float AlphaB { get; }
    float AlphaA { get; }
    Vector2 Scale { get; }
    Vector3 Position1 { get; }
    Vector3 Position2 { get; }
    ITextureInfo Model { get; }
    ITextureInfo Texture { get; }
    IEnumerable<IMesh> SubMeshes { get; }
  }
}