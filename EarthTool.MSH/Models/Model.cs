using EarthTool.Common.Extensions;
using EarthTool.MSH.Models.Collections;
using EarthTool.MSH.Models.Elements;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace EarthTool.MSH.Models
{
  public class Model
  {
    private static readonly byte[] MeshIdentifier = new byte[] { 0x4d, 0x45, 0x53, 0x48, 0x01, 0x00, 0x00, 0x00 };

    public string FilePath
    {
      get; set;
    }

    public int Type
    {
      get; set;
    }

    public ModelTemplate Template
    {
      get; set;
    }

    public int BuildingFrames
    {
      get; set;
    }

    public int ActionFrames
    {
      get; set;
    }

    public int MovementFrames
    {
      get; set;
    }

    public int LoopedFrames
    {
      get; set;
    }

    public MountPoints MountPoints
    {
      get; set;
    }

    public SpotLights SpotLights
    {
      get; set;
    }

    public OmniLights OmniLights
    {
      get; set;
    }

    public Slots Slots
    {
      get; set;
    }

    public short MaxY
    {
      get; set;
    }

    public short MinY
    {
      get; set;
    }

    public short MaxX
    {
      get; set;
    }

    public short MinX
    {
      get; set;
    }

    public int UnknownVal5
    {
      get; set;
    }

    public IEnumerable<ModelPart> Parts
    {
      get; set;
    }

    public PartNode PartsTree
    {
      get; set;
    }

    public Model(string path)
    {
      FilePath = path;

      using (var stream = new FileStream(path, FileMode.Open))
      {
        FindHeader(stream);
        Type = BitConverter.ToInt32(stream.ReadBytes(4));
        Template = new ModelTemplate(stream);
        BuildingFrames = stream.ReadByte();
        ActionFrames = stream.ReadByte();
        MovementFrames = stream.ReadByte();
        LoopedFrames = stream.ReadByte();
        stream.ReadBytes(4); //EMPTY
        MountPoints = new MountPoints(stream);
        SpotLights = new SpotLights(stream);
        OmniLights = new OmniLights(stream);
        stream.ReadBytes(96);
        Slots = new Slots(stream);
        MaxY = BitConverter.ToInt16(stream.ReadBytes(2));
        MinY = BitConverter.ToInt16(stream.ReadBytes(2));
        MaxX = BitConverter.ToInt16(stream.ReadBytes(2));
        MinX = BitConverter.ToInt16(stream.ReadBytes(2));
        UnknownVal5 = BitConverter.ToInt32(stream.ReadBytes(4));
        if (Type != 0)
        {
          throw new NotSupportedException("Not supported mesh format");
        }
        Parts = GetParts(stream).ToList();
        PartsTree = GetPartsTree(Parts);
      }
    }

    public byte[] ToByteArray()
    {
      using (var output = new MemoryStream())
      {
        using (var bw = new BinaryWriter(output, Encoding.GetEncoding("ISO-8859-2")))
        {
          bw.Write(MeshIdentifier);
          bw.Write(Type);
          bw.Write(Template.ToByteArray());
          bw.Write((byte)BuildingFrames);
          bw.Write((byte)ActionFrames);
          bw.Write((byte)MovementFrames);
          bw.Write((byte)LoopedFrames);
          bw.Write(new byte[4]);
          bw.Write(MountPoints.ToByteArray());
          bw.Write(SpotLights.ToByteArray());
          bw.Write(OmniLights.ToByteArray());
          bw.Write(new byte[96]);
          bw.Write(Slots.ToByteArray());
          bw.Write(MaxY);
          bw.Write(MinY);
          bw.Write(MaxX);
          bw.Write(MinX);
          bw.Write(UnknownVal5);
          bw.Write(Parts.SelectMany(p => p.ToByteArray()).ToArray());
        }
        return output.ToArray();
      }
    }

    private PartNode GetPartsTree(IEnumerable<ModelPart> parts)
    {
      var currentId = 0;
      var root = new PartNode(currentId, parts.First());
      var lastNode = root;
      foreach (var part in parts.Skip(1))
      {
        var skip = part.SkipParent;
        var parent = lastNode;
        for (var i = 0; i < skip; i++)
        {
          parent = parent.Parent;
        }
        lastNode = new PartNode(++currentId, part, parent);
      }
      return root;
    }

    private void FindHeader(Stream stream)
    {
      var type = stream.ReadBytes(32).AsSpan();
      var pos = type.IndexOf(MeshIdentifier);
      if (pos == -1)
      {
        throw new NotSupportedException("Unhandled file format");
      }
      stream.Seek(pos + MeshIdentifier.Length, SeekOrigin.Begin);
    }

    private IEnumerable<ModelPart> GetParts(Stream stream)
    {
      while (stream.Position < stream.Length)
      {
        yield return new ModelPart(stream);
      }
    }
  }
}
