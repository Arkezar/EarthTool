using EarthTool.MSH.Interfaces;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Numerics;
using System.Text;

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

    public byte[] ToByteArray(Encoding encoding)
    {
      using (var output = new MemoryStream())
      {
        using (var bw = new BinaryWriter(output, encoding))
        {
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
          bw.Write(Additive);
          bw.Write(LightColor.ToArgb());
          bw.Write(Color.ToArgb());
          bw.Write(ColorIntensity);
          bw.Write(AlphaInt);
          bw.Write(AlphaB);
          bw.Write(AlphaA);
          bw.Write(Scale.X);
          bw.Write(Scale.Y);
          bw.Write(Position1.X);
          bw.Write(Position1.Y);
          bw.Write(Position1.Z);
          bw.Write(Position2.X);
          bw.Write(Position2.Y);
          bw.Write(Position2.Z);
          bw.Write(Model.ToByteArray(encoding));
          bw.Write(Texture.ToByteArray(encoding));
          bw.Write(SubMeshes.Count());
          foreach (var subMesh in SubMeshes)
          {
            bw.Write(subMesh.ToByteArray(encoding));
          }
        }

        return output.ToArray().ToArray();
      }
    }
  }
}