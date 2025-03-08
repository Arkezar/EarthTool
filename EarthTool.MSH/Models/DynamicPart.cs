using EarthTool.MSH.Interfaces;
using System.Collections.Generic;
using System.Drawing;
using System.Numerics;

namespace EarthTool.MSH.Models
{
  public class DynamicPart : IDynamicPart
  {
    public LightType LightType { get; set; }
    public int SpriteStartIndex { get; set; }
    public int SpriteAnimationLength { get; set; }
    public int SpriteSheetVertical { get; set; }
    public int SpriteSheetHorizontal { get; set; }
    public int Framerate { get; set; }
    public float TextureSplitRatioVertical { get; set; }
    public float TextureSplitRatioHorizontal { get; set; }
    public ISize Size1 { get; set; }
    public ISize Size2 { get; set; }
    public float SizeZ { get; set; }
    public float Radius { get; set; }
    public int Unknown { get; set; }
    public bool Additive { get; set; }
    public Color LightColor { get; set; }
    public Color Color { get; set; }
    public float ColorIntensity { get; set; }
    public int AlphaInt { get; set; }
    public float AlphaB { get; set; }
    public float AlphaA { get; set; }
    public Vector2 Scale { get; set; }
    public Vector3 Position1 { get; set; }
    public Vector3 Position2 { get; set; }
    public ITextureInfo Model { get; set; }
    public ITextureInfo Texture { get; set; }
    public IEnumerable<IMesh> SubMeshes { get; set; }
  }
}