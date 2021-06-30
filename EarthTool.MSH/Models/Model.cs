using EarthTool.Common.Extensions;
using EarthTool.MSH.Models.Collections;
using EarthTool.MSH.Models.Elements;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace EarthTool.MSH.Models
{
  public class Model
  {
    public string FilePath
    {
      get;
    }

    public int Type
    {
      get;
    }

    public ModelTemplate Template
    {
      get;
    }

    public MountPoints MountPoints
    {
      get;
    }

    public Lights Lights
    {
      get;
    }

    public short UnknownVal1
    {
      get;
    }

    public short UnknownVal2
    {
      get;
    }

    public short UnknownVal3
    {
      get;
    }

    public short UnknownVal4
    {
      get;
    }

    public int UnknownVal5
    {
      get;
    }

    public IEnumerable<ModelPart> Parts
    {
      get;
    }

    public PartNode PartsTree
    {
      get;
    }

    public Model(string path)
    {
      Parts = new List<ModelPart>();
      FilePath = path;

      using (var stream = new FileStream(path, FileMode.Open))
      {
        CheckHeader(stream);
        Type = BitConverter.ToInt32(stream.ReadBytes(4));
        Template = new ModelTemplate(stream);
        stream.ReadBytes(10);
        MountPoints = new MountPoints(stream);
        Lights = new Lights(stream);
        stream.ReadBytes(64);
        stream.ReadBytes(488);
        UnknownVal1 = BitConverter.ToInt16(stream.ReadBytes(2));
        UnknownVal2 = BitConverter.ToInt16(stream.ReadBytes(2));
        UnknownVal3 = BitConverter.ToInt16(stream.ReadBytes(2));
        UnknownVal4 = BitConverter.ToInt16(stream.ReadBytes(2));
        UnknownVal5 = BitConverter.ToInt16(stream.ReadBytes(4));
        if (Type != 0)
        {
          throw new NotSupportedException("Not supported mesh format");
        }
        Parts = GetParts(stream).ToList();
        PartsTree = GetPartsTree(Parts);
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

    private void CheckHeader(Stream stream)
    {
      var type = stream.ReadBytes(8).AsSpan();
      if (!type.SequenceEqual(new byte[] { 0x4d, 0x45, 0x53, 0x48, 0x01, 0x00, 0x00, 0x00 }))
      {
        throw new NotSupportedException("Unhandled file format");
      }
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
