﻿using EarthTool.Common;
using EarthTool.Common.Interfaces;
using EarthTool.MSH.Interfaces;
using EarthTool.MSH.Models;
using EarthTool.MSH.Models.Collections;
using EarthTool.MSH.Models.Elements;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;

namespace EarthTool.MSH.Services
{
  public class EarthMeshReader : IMeshReader
  {
    private readonly IEarthInfoFactory _earthInfoFactory;
    private readonly Encoding _encoding;

    public EarthMeshReader(IEarthInfoFactory earthInfoFactory, Encoding encoding)
    {
      _earthInfoFactory = earthInfoFactory;
      _encoding = encoding;
    }

    public IMesh Read(string filePath)
      => ReadStream(filePath);

    private IMesh ReadStream(string filePath)
    {
      if (File.Exists(filePath))
      {
        using (var stream = File.OpenRead(filePath))
        {
          var mesh = new EarthMesh();
          mesh.FileHeader = _earthInfoFactory.Get(stream);
          using (var reader = new BinaryReader(stream, _encoding))
          {
            IsValidModel(reader);
            mesh.Descriptor = LoadMeshDescriptor(reader);

            if (mesh.Descriptor.Type != 0)
            {
              throw new NotSupportedException("Not supported mesh format");
            }

            mesh.Geometries = LoadParts(reader).ToList();
            mesh.PartsTree = GetPartsTree(mesh.Geometries);
          }
          return mesh;
        }
      }
      return default;
    }

    #region Descriptor

    private IMeshDescriptor LoadMeshDescriptor(BinaryReader reader)
      => new MeshDescriptor
      {
        Type = reader.ReadInt32(),
        Template = LoadModelTemplate(reader),
        Frames = LoadMeshFrames(reader),
        UnknownValue1 = reader.ReadInt32(),
        MountPoints = LoadSlotList(reader, 4, (reader, _) => LoadVector(reader)),
        SpotLights = LoadSlotList(reader, 4, (reader, _) => LoadSpotLight(reader)),
        OmniLights = LoadSlotList(reader, 4, (reader, _) => LoadOmniLight(reader)),
        TemplateDetails = LoadTemplateDetails(reader),
        Slots = LoadModelSlots(reader),
        Boundries = LoadMeshBoundries(reader),
        UnknownValue2 = BitConverter.ToInt32(reader.ReadBytes(4))
      };

    private IModelSlots LoadModelSlots(BinaryReader reader)
      => new ModelSlots()
      {
        Turrets = LoadSlotList(reader, 4, (reader, i) => LoadSlot(reader, i)),
        BarrelMuzzels = LoadSlotList(reader, 4, (reader, i) => LoadSlot(reader, i)),
        TurretMuzzels = LoadSlotList(reader, 4, (reader, i) => LoadSlot(reader, i)),
        Headlights = LoadSlotList(reader, 4, (reader, i) => LoadSlot(reader, i)),
        Omnilights = LoadSlotList(reader, 4, (reader, i) => LoadSlot(reader, i)),
        UnloadPoints = LoadSlotList(reader, 4, (reader, i) => LoadSlot(reader, i)),
        HitSpots = LoadSlotList(reader, 4, (reader, i) => LoadSlot(reader, i)),
        SmokeSpots = LoadSlotList(reader, 4, (reader, i) => LoadSlot(reader, i)),
        Unknown = LoadSlotList(reader, 4, (reader, i) => LoadSlot(reader, i)),
        Chimneys = LoadSlotList(reader, 2, (reader, i) => LoadSlot(reader, i)),
        SmokeTraces = LoadSlotList(reader, 2, (reader, i) => LoadSlot(reader, i)),
        Exhausts = LoadSlotList(reader, 2, (reader, i) => LoadSlot(reader, i)),
        KeelTraces = LoadSlotList(reader, 2, (reader, i) => LoadSlot(reader, i)),
        InterfacePivot = LoadSlotList(reader, 1, (reader, i) => LoadSlot(reader, i)),
        CenterPivot = LoadSlotList(reader, 1, (reader, i) => LoadSlot(reader, i)),
        ProductionSpotStart = LoadSlotList(reader, 1, (reader, i) => LoadSlot(reader, i)),
        ProductionSpotEnd = LoadSlotList(reader, 1, (reader, i) => LoadSlot(reader, i)),
        LandingSpot = LoadSlotList(reader, 1, (reader, i) => LoadSlot(reader, i))
      };

    private ISlot LoadSlot(BinaryReader reader, int id)
    {
      var x = reader.ReadInt16() / 255f;
      var y = -reader.ReadInt16() / 255f;
      var z = reader.ReadInt16() / 255f;
      var result = new Slot(id)
      {
        Position = new Vector(x, y, z),
        Direction = reader.ReadByte() / 255.0 * Math.PI * 2.0,
        Flag = reader.ReadByte()
      };
      return result;
    }

    private ModelTemplate LoadModelTemplate(BinaryReader reader)
    {
      var template = new ModelTemplate();

      var data = new BitArray(reader.ReadBytes(2));
      for (var col = ModelTemplate.COLUMNS - 1; col > -1; col--)
      {
        for (var row = ModelTemplate.ROWS - 1; row > -1; row--)
        {
          template.Matrix[row, ModelTemplate.COLUMNS - 1 - col] = data[col * 4 + row];
        }
      }

      template.Flag = reader.ReadInt16();

      return template;
    }

    private IMeshFrames LoadMeshFrames(BinaryReader reader)
      => new MeshFrames
      {
        BuildingFrames = reader.ReadByte(),
        ActionFrames = reader.ReadByte(),
        MovementFrames = reader.ReadByte(),
        LoopedFrames = reader.ReadByte()
      };

    private IMeshBoundries LoadMeshBoundries(BinaryReader reader)
      => new MeshBoundries
      {
        MaxY = reader.ReadInt16(),
        MinY = reader.ReadInt16(),
        MaxX = reader.ReadInt16(),
        MinX = reader.ReadInt16()
      };

    private ITemplateDetails LoadTemplateDetails(BinaryReader reader)
      => new TemplateDetails()
      {
        SectionHeights = GetSectionHeights(reader),
        SectionFlags = GetSectionFlags(reader),
        SectionRotations = LoadSlotList(reader, 4, (reader, _) => LoadModelTemplate(reader)),
        SectionFlagRotations = LoadSlotList(reader, 4, (reader, _) => LoadSectionFlagRotation(reader))
      };

    private IEnumerable<T> LoadSlotList<T>(BinaryReader reader, int count, Func<BinaryReader, int, T> load)
    {
      return Enumerable.Range(0, count).Select(i => load(reader, i)).ToArray();
    }

    private SpotLight LoadSpotLight(BinaryReader reader)
    {
      return new SpotLight()
      {
        Value = GetVector(reader),
        Color = GetColor(reader),
        Length = reader.ReadSingle(),
        Direction = reader.ReadInt32(),
        Width = reader.ReadSingle(),
        U3 = reader.ReadSingle(),
        Tilt = reader.ReadSingle(),
        Ambience = reader.ReadSingle()
      };
    }

    private IOmniLight LoadOmniLight(BinaryReader reader)
    {
      return new OmniLight()
      {
        Value = GetVector(reader),
        Color = GetColor(reader),
        Radius = reader.ReadSingle()
      };
    }

    private Color GetColor(BinaryReader reader)
    {
      var r = reader.ReadSingle() * 255;
      var g = reader.ReadSingle() * 255;
      var b = reader.ReadSingle() * 255;
      return Color.FromArgb((int)r, (int)g, (int)b);
    }

    private short[,] GetSectionHeights(BinaryReader reader)
    {
      var sectionHeights = new short[4, 4];

      for (var row = 0; row < 4; row++)
      {
        for (var col = 0; col < 4; col++)
        {
          sectionHeights[row, col] = reader.ReadInt16();
        }
      }

      return sectionHeights;
    }

    private byte[,] GetSectionFlags(BinaryReader reader)
    {
      var sectionFlags = new byte[4, 4];

      for (var row = 0; row < 4; row++)
      {
        for (var col = 0; col < 4; col++)
        {
          sectionFlags[row, col] = reader.ReadByte();
        }
      }

      return sectionFlags;
    }

    private byte[,] LoadSectionFlagRotation(BinaryReader reader)
    {
      var rotation = new byte[4, 4];

      for (var i = 0; i < 4; i++)
      {
        var columnValue = reader.ReadInt16();
        var upperByte = (byte)(columnValue >> 8);
        var lowerByte = (byte)(columnValue & 0xFF);
        var r0 = (byte)(upperByte >> 4);
        var r1 = (byte)(upperByte & 0xF);
        var r2 = (byte)(lowerByte >> 4);
        var r3 = (byte)(lowerByte & 0xF);
        rotation[i, 0] = r0;
        rotation[i, 1] = r1;
        rotation[i, 2] = r2;
        rotation[i, 3] = r3;
      }
      return rotation;
    }

    #endregion

    #region Geometry

    private IEnumerable<IModelPart> LoadParts(BinaryReader reader)
    {
      while (reader.BaseStream.Position < reader.BaseStream.Length)
      {
        yield return LoadPart(reader);
      }
    }

    private IModelPart LoadPart(BinaryReader reader)
    {
      var result = new ModelPart();
      result.Vertices = LoadVertices(reader);
      result.Depth = reader.ReadByte();
      result.PartType = reader.ReadByte();
      result.UnknownFlag2 = reader.ReadInt16();
      result.Texture = LoadTextureInfo(reader);
      result.Faces = LoadFaces(reader);
      result.Animations = LoadAnimations(reader);
      result.UnknownValue = reader.ReadInt32();
      result.Offset = LoadVector(reader);
      result.UnknownBytes = reader.ReadBytes(5);
      return result;
    }

    private IAnimations LoadAnimations(BinaryReader reader)
      => new Animations()
      {
        UnknownAnimationData = LoadSlotList(reader, reader.ReadInt32(), (reader, _) => LoadVector(reader)),
        MovementFrames = LoadSlotList(reader, reader.ReadInt32(), (reader, _) => LoadVector(reader)),
        RotationFrames = LoadSlotList(reader, reader.ReadInt32(), (reader, _) => LoadRotationFrame(reader)),
      };

    private IRotationFrame LoadRotationFrame(BinaryReader reader)
      => new RotationFrame()
      {
        TransformationMatrix = new System.Numerics.Matrix4x4()
        {
          M11 = reader.ReadSingle(),
          M12 = -reader.ReadSingle(),
          M13 = -reader.ReadSingle(),
          M14 = reader.ReadSingle(),
          M21 = -reader.ReadSingle(),
          M22 = reader.ReadSingle(),
          M23 = -reader.ReadSingle(),
          M24 = reader.ReadSingle(),
          M31 = -reader.ReadSingle(),
          M32 = -reader.ReadSingle(),
          M33 = reader.ReadSingle(),
          M34 = reader.ReadSingle(),
          M41 = reader.ReadSingle(),
          M42 = reader.ReadSingle(),
          M43 = reader.ReadSingle(),
          M44 = reader.ReadSingle()
        }
      };

    private IEnumerable<IFace> LoadFaces(BinaryReader reader)
    {
      var faces = reader.ReadInt32();
      return Enumerable.Range(0, faces).Select(_ => LoadFace(reader)).ToArray();
    }

    private IFace LoadFace(BinaryReader reader)
      => new Face()
      {
        V1 = reader.ReadInt16(),
        V2 = reader.ReadInt16(),
        V3 = reader.ReadInt16(),
        UNKNOWN = reader.ReadInt16(),
      };

    private ITextureInfo LoadTextureInfo(BinaryReader reader)
    {
      var fileNameLength = reader.ReadInt32();
      var fileName = Encoding.GetEncoding("ISO-8859-2").GetString(reader.ReadBytes(fileNameLength));
      return new TextureInfo()
      {
        FileName = fileName
      };
    }

    private IEnumerable<IVertex> LoadVertices(BinaryReader reader)
    {
      var vertices = reader.ReadInt32();
      var blocks = reader.ReadInt32();

      return Enumerable.Range(0, blocks).SelectMany(_ => GetVertices(reader.ReadBytes(160))).Take(vertices).ToArray();
    }

    private IEnumerable<IVertex> GetVertices(byte[] vertexData)
    {
      for (var i = 0; i < 4; i++)
      {
        var idx = i * sizeof(float);

        var x = BitConverter.ToSingle(vertexData, idx + 0x00);
        var y = -BitConverter.ToSingle(vertexData, idx + 0x10);
        var z = BitConverter.ToSingle(vertexData, idx + 0x20);

        var normalX = BitConverter.ToSingle(vertexData, idx + 0x30);
        var normalY = -BitConverter.ToSingle(vertexData, idx + 0x40);
        var normalZ = BitConverter.ToSingle(vertexData, idx + 0x50);

        var u = BitConverter.ToSingle(vertexData, idx + 0x60);
        var v = 1 - BitConverter.ToSingle(vertexData, idx + 0x70);

        var u1 = BitConverter.ToInt16(vertexData, i * sizeof(short) + 0x90);
        var u2 = BitConverter.ToInt16(vertexData, i * sizeof(short) + 0x98);
        yield return new Vertex(new Vector(x, y, z), new Vector(normalX, normalY, normalZ), new UVMap(u, v), u1, u2);
      }
    }

    private PartNode GetPartsTree(IEnumerable<IModelPart> parts)
    {
      var currentId = 0;
      var root = new PartNode(currentId, parts.First());
      var lastNode = root;
      foreach (var part in parts.Skip(1))
      {
        var skip = part.Depth;
        var parent = lastNode;
        for (var i = 0; i < skip; i++)
        {
          parent = parent.Parent;
        }
        lastNode = new PartNode(++currentId, part, parent);
        if (part.PartType == 0)
        {
          lastNode = parent;
        }
      }
      return root;
    }

    #endregion

    #region Common

    private IVector LoadVector(BinaryReader reader)
    {
      return new Vector()
      {
        Value = GetVector(reader),
      };
    }

    private System.Numerics.Vector3 GetVector(BinaryReader reader)
    {
      var x = reader.ReadSingle();
      var y = -reader.ReadSingle();
      var z = reader.ReadSingle();
      return new System.Numerics.Vector3(x, y, z);
    }

    private void IsValidModel(BinaryReader reader)
    {
      var valid = reader.ReadBytes(Identifiers.Mesh.Length).AsSpan().SequenceEqual(Identifiers.Mesh);
      if (!valid)
      {
        throw new NotSupportedException("Unhandled file format");
      }
    }

    #endregion
  }
}