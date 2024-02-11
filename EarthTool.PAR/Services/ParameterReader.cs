using EarthTool.Common;
using EarthTool.Common.Bases;
using EarthTool.Common.Enums;
using EarthTool.Common.Interfaces;
using EarthTool.PAR.Models;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace EarthTool.PAR.Services
{
  public class ParameterReader : Reader<ParFile>
  {
    private readonly IEarthInfoFactory _earthInfoFactory;
    private readonly Encoding _encoding;
    public override FileType InputFileExtension => FileType.PAR;

    public ParameterReader(IEarthInfoFactory earthInfoFactory, Encoding encoding)
    {
      _earthInfoFactory = earthInfoFactory;
      _encoding = encoding;
    }

    protected override ParFile InternalRead(string filePath)
    {
      using (var stream = File.OpenRead(filePath))
      {
        var mesh = new ParFile();
        mesh.FileHeader = _earthInfoFactory.Get(stream);
        using (var reader = new BinaryReader(stream, _encoding))
        {
          IsValidModel(reader);
          mesh.Groups = LoadGroups(reader);
          mesh.Research = LoadResearch(reader);
        }

        return mesh;
      }
    }

    private static IEnumerable<Research> LoadResearch(BinaryReader reader)
    {
      var researchCount = reader.ReadInt32();
      reader.ReadInt32();
      return Enumerable.Range(0, researchCount).Select(i => new Research(reader)).ToList();
    }

    private static IEnumerable<EntityGroup> LoadGroups(BinaryReader reader)
    {
      var groupCount = reader.ReadInt32();
      reader.ReadInt32();
      return Enumerable.Range(0, groupCount).Select(i => new EntityGroup(reader)).ToList();
    }

    private void IsValidModel(BinaryReader reader)
    {
      var valid = reader.ReadBytes(Identifiers.Paramters.Length).AsSpan().SequenceEqual(Identifiers.Paramters);
      if (!valid)
      {
        throw new NotSupportedException("Unhandled file format");
      }
    }
  }
}