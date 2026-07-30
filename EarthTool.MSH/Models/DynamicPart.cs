using EarthTool.MSH.Interfaces;
using EarthTool.MSH.Models.Elements;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Numerics;
using System.Text;

namespace EarthTool.MSH.Models
{
  public class DynamicPart : IDynamicPart
  {
    public DynamicPart()
    {
      EffectType = EffectType.Unknown;
      LightType = LightType.Const;
      Size1 = CreateDefaultSize();
      Size2 = CreateDefaultSize();
      SizeZ = 0.25f;
      Radius = 0.25f;
      ColorRgb = Vector3.One;
      ColorParameter = 1f;
      AlphaB = 1f;
      AlphaA = 1f;
      Position1 = new Vector3(0f, -0f, 0f);
      Position2 = new Vector3(0f, -0f, 0f);
      Model = new TextureInfo { FileName = string.Empty };
      Texture = new TextureInfo { FileName = string.Empty };
      SubMeshes = Array.Empty<IMesh>();
    }

    public EffectType EffectType { get; set; }
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
    public Vector3 LightVector { get; set; }
    public Vector3 ColorRgb { get; set; }
    public float ColorParameter { get; set; }
    public int AlphaInt { get; set; }
    public float AlphaB { get; set; }
    public float AlphaA { get; set; }
    public Vector2 Scale { get; set; }
    public Vector3 Position1 { get; set; }
    public Vector3 Position2 { get; set; }
    public ITextureInfo Model { get; set; }
    public ITextureInfo Texture { get; set; }
    public IEnumerable<IMesh> SubMeshes { get; set; }

    public byte[] ToByteArray(Encoding encoding)
    {
      using (var output = new MemoryStream())
      {
        using (var bw = new BinaryWriter(output, encoding))
        {
          bw.Write((int)EffectType);
          bw.Write((int)LightType);
          bw.Write(SpriteStartIndex);
          bw.Write(SpriteAnimationLength);
          bw.Write(SpriteSheetVertical);
          bw.Write(SpriteSheetHorizontal);
          bw.Write(Framerate);
          bw.Write(TextureSplitRatioVertical);
          bw.Write(TextureSplitRatioHorizontal);
          bw.Write(Size1.ToByteArray(encoding));
          bw.Write(Size2.ToByteArray(encoding));
          bw.Write(SizeZ);
          bw.Write(Radius);
          bw.Write(Unknown);
          bw.Write(Additive ? 1 : 0);
          bw.Write(LightVector.X);
          bw.Write(LightVector.Y);
          bw.Write(LightVector.Z);
          bw.Write(ColorRgb.X);
          bw.Write(ColorRgb.Y);
          bw.Write(ColorRgb.Z);
          bw.Write(ColorParameter);
          bw.Write(AlphaInt);
          bw.Write(AlphaB);
          bw.Write(AlphaA);
          bw.Write(Scale.X);
          bw.Write(Scale.Y);
          bw.Write(Position1.X);
          bw.Write(-Position1.Y);
          bw.Write(Position1.Z);
          bw.Write(Position2.X);
          bw.Write(-Position2.Y);
          bw.Write(Position2.Z);
          bw.Write(Model.ToByteArray(encoding));
          bw.Write(Texture.ToByteArray(encoding));
          bw.Write(SubMeshes.Count());
          foreach (var subMesh in SubMeshes)
          {
            bw.Write(EarthMesh.ToNestedDynamicByteArray(subMesh, encoding));
          }
        }

        return output.ToArray().ToArray();
      }
    }

    private static Size CreateDefaultSize()
    {
      return new Size()
      {
        X1 = -0.25f,
        X2 = 0.25f,
        Y1 = 0.25f,
        Y2 = -0.25f
      };
    }
  }
}
