using EarthTool.Common;
using EarthTool.Common.Bases;
using EarthTool.Common.Enums;
using EarthTool.Common.Interfaces;
using EarthTool.Common.Models;
using EarthTool.MSH.Enums;
using EarthTool.MSH.Interfaces;
using EarthTool.MSH.Models;
using EarthTool.MSH.Models.Collections;
using EarthTool.MSH.Models.Elements;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Numerics;
using System.Text;
using Size = EarthTool.MSH.Models.Size;
using Vector = EarthTool.MSH.Models.Elements.Vector;

namespace EarthTool.MSH.Services
{
  public class EarthMeshReader : Reader<IMesh>
  {
    private const uint StaticArchiveFraming = 0x20D0A1FF;
    private const uint DynamicArchiveFraming = 0x30D0A1FF;

    private readonly IEarthInfoFactory _earthInfoFactory;
    private readonly IHierarchyBuilder _hierarchyBuilder;
    private readonly Encoding _encoding;

    public EarthMeshReader(IEarthInfoFactory earthInfoFactory, IHierarchyBuilder hierarchyBuilder, Encoding encoding)
    {
      _earthInfoFactory = earthInfoFactory;
      _hierarchyBuilder = hierarchyBuilder;
      _encoding = encoding;
    }

    public override FileType InputFileExtension => FileType.MSH;

    protected override IMesh InternalRead(string filePath)
    {
      using (var stream = File.OpenRead(filePath))
      using (var reader = new BinaryReader(stream, _encoding))
      {
        try
        {
          var archive = LoadArchiveHeader(reader);
          var mesh = new EarthMesh
          {
            FileHeader = archive.Header,
            BaseHeader = LoadMeshBaseHeader(reader)
          };

          if (mesh.BaseHeader.MeshKind != archive.ExpectedKind)
          {
            throw InvalidData("MeshKind", stream.Position - MeshBaseHeader.SerializedSize + 0x08,
              $"archive framing requires {archive.ExpectedKind}, found {mesh.BaseHeader.MeshKind}");
          }

          if (mesh.BaseHeader.MeshKind == MeshKind.Static)
          {
            var trailingUnwindOffset = stream.Position;
            var storedTrailingUnwind = ReadUInt32(reader, "TrailingHierarchyUnwindCount");
            mesh.Geometries = LoadStaticParts(reader, out var finalSourceDepth);
            var expectedTrailingUnwind = (uint)finalSourceDepth + 1;
            if (storedTrailingUnwind != expectedTrailingUnwind)
            {
              throw InvalidData("TrailingHierarchyUnwindCount", trailingUnwindOffset,
                $"expected {expectedTrailingUnwind}, found {storedTrailingUnwind}");
            }

            mesh.PartsTree = _hierarchyBuilder.GetPartsTree(mesh.Geometries);
          }
          else
          {
            mesh.RootDynamic = LoadEffect(reader);
            RequireExactEnd(reader, "dynamic root record");
          }

          return mesh;
        }
        catch (InvalidDataException)
        {
          throw;
        }
        catch (EndOfStreamException ex)
        {
          throw InvalidData("MSH field", stream.Position, "unexpected end of file", ex);
        }
        catch (ArgumentException ex)
        {
          throw InvalidData("MSH field", stream.Position, ex.Message, ex);
        }
      }
    }

    private (IEarthInfo Header, MeshKind ExpectedKind) LoadArchiveHeader(BinaryReader reader)
    {
      var framing = ReadUInt32(reader, "ArchiveFraming");
      if (framing == StaticArchiveFraming)
      {
        var guid = new Guid(ReadExact(reader, 16, "ArchiveGuid"));
        return (_earthInfoFactory.Get(FileFlags.Guid, guid), MeshKind.Static);
      }

      if (framing == DynamicArchiveFraming)
      {
        var resourceOffset = reader.BaseStream.Position;
        var resourceType = ReadUInt32(reader, "ArchiveResourceType");
        if (resourceType != (uint)ResourceType.Effect)
        {
          throw InvalidData("ArchiveResourceType", resourceOffset,
            $"expected {(uint)ResourceType.Effect}, found {resourceType}");
        }

        var guid = new Guid(ReadExact(reader, 16, "ArchiveGuid"));
        return (_earthInfoFactory.Get(FileFlags.Resource | FileFlags.Guid, guid, ResourceType.Effect), MeshKind.Dynamic);
      }

      throw InvalidData("ArchiveFraming", 0, $"expected 0x{StaticArchiveFraming:X8} or 0x{DynamicArchiveFraming:X8}, found 0x{framing:X8}");
    }

    private IDynamicPart LoadEffect(BinaryReader reader)
      => new DynamicPart
      {
        EffectType = (EffectType)reader.ReadInt32(),
        LightType = (LightType)reader.ReadInt32(),
        SpriteStartIndex = reader.ReadInt32(),
        SpriteAnimationLength = reader.ReadInt32(),
        SpriteSheetVertical = reader.ReadInt32(),
        SpriteSheetHorizontal = reader.ReadInt32(),
        Framerate = reader.ReadInt32(),
        TextureSplitRatioVertical = reader.ReadSingle(),
        TextureSplitRatioHorizontal = reader.ReadSingle(),
        Size1 =
          new Size()
          {
            X1 = reader.ReadSingle(),
            X2 = reader.ReadSingle(),
            Y1 = reader.ReadSingle(),
            Y2 = reader.ReadSingle()
          },
        Size2 =
          new Size()
          {
            X1 = reader.ReadSingle(),
            X2 = reader.ReadSingle(),
            Y1 = reader.ReadSingle(),
            Y2 = reader.ReadSingle()
          },
        SizeZ = reader.ReadSingle(),
        Radius = reader.ReadSingle(),
        Unknown = reader.ReadInt32(),
        Additive = reader.ReadInt32() > 0,
        LightColor = GetColor(reader),
        Color = GetColor(reader),
        ColorIntensity = reader.ReadSingle(),
        AlphaInt = reader.ReadInt32(),
        AlphaB = reader.ReadSingle(),
        AlphaA = reader.ReadSingle(),
        Scale = new Vector2(reader.ReadSingle(), reader.ReadSingle()),
        Position1 = new Vector3(reader.ReadSingle(), reader.ReadSingle(), reader.ReadSingle()),
        Position2 = new Vector3(reader.ReadSingle(), reader.ReadSingle(), reader.ReadSingle()),
        Model = LoadTextureInfo(reader),
        Texture = LoadTextureInfo(reader),
        SubMeshes = LoadSubMeshes(reader)
      };

    private IEnumerable<IMesh> LoadSubMeshes(BinaryReader reader)
    {
      var count = reader.ReadInt32();
      return Enumerable.Range(0, count).Select(_ => LoadMesh(reader)).ToArray();
    }

    private IMesh LoadMesh(BinaryReader reader)
    {
      var mesh = new EarthMesh
      {
        FileHeader = _earthInfoFactory.Get(),
        BaseHeader = LoadMeshBaseHeader(reader)
      };
      if (mesh.BaseHeader.MeshKind != MeshKind.Dynamic)
      {
        throw InvalidData("MeshKind", reader.BaseStream.Position - MeshBaseHeader.SerializedSize + 0x08,
          $"nested dynamic object requires {MeshKind.Dynamic}, found {mesh.BaseHeader.MeshKind}");
      }
      mesh.RootDynamic = LoadEffect(reader);
      return mesh;
    }

    #region Descriptor

    private IMeshBaseHeader LoadMeshBaseHeader(BinaryReader reader)
    {
      var start = reader.BaseStream.Position;
      EnsureRemaining(reader, MeshBaseHeader.SerializedSize, "BaseHeader");
      var magic = reader.ReadBytes(4);
      if (!magic.AsSpan().SequenceEqual(Identifiers.Mesh.AsSpan(0, 4)))
      {
        throw InvalidData("BaseHeader.Magic", start, "expected MESH");
      }

      var version = reader.ReadUInt32();
      if (version != MeshBaseHeader.SupportedVersion)
      {
        throw InvalidData("BaseHeader.Version", start + 0x04,
          $"expected {MeshBaseHeader.SupportedVersion}, found {version}");
      }

      var kindValue = reader.ReadInt32();
      if (!Enum.IsDefined(typeof(MeshKind), kindValue))
      {
        throw InvalidData("BaseHeader.MeshKind", start + 0x08, $"unsupported value {kindValue}");
      }

      var result = new MeshBaseHeader
      {
        MeshKind = (MeshKind)kindValue,
        BoxPresenceMask = reader.ReadUInt32(),
        Frames = LoadMeshFrames(reader),
        HeaderFlags = reader.ReadInt32(),
        MountPoints = LoadSlotList(reader, 4, (r, _) => LoadVector(r)),
        SpotLights = LoadSlotList(reader, 4, (r, _) => LoadSpotLight(r)),
        OmnidirectionalLights = LoadSlotList(reader, 4, (r, _) => LoadOmniLight(r)),
        Footprint = LoadMeshFootprint(reader),
        Slots = LoadModelSlots(reader),
        HorizontalExtents = LoadMeshHorizontalExtents(reader)
      };

      var actualSize = reader.BaseStream.Position - start;
      if (actualSize != MeshBaseHeader.SerializedSize)
      {
        throw InvalidData("BaseHeader", start,
          $"expected size 0x{MeshBaseHeader.SerializedSize:X}, consumed 0x{actualSize:X}");
      }

      return result;
    }

    private IModelSlots LoadModelSlots(BinaryReader reader)
      => new ModelSlots()
      {
        Turrets = LoadSlotList(reader, 4, (r, i) => LoadSlot(r, i)),
        BarrelMuzzels = LoadSlotList(reader, 4, (r, i) => LoadSlot(r, i)),
        TurretMuzzels = LoadSlotList(reader, 4, (r, i) => LoadSlot(r, i)),
        Headlights = LoadSlotList(reader, 4, (r, i) => LoadSlot(r, i)),
        Omnilights = LoadSlotList(reader, 4, (r, i) => LoadSlot(r, i)),
        UnloadPoints = LoadSlotList(reader, 4, (r, i) => LoadSlot(r, i)),
        HitSpots = LoadSlotList(reader, 4, (r, i) => LoadSlot(r, i)),
        SmokeSpots = LoadSlotList(reader, 4, (r, i) => LoadSlot(r, i)),
        Unknown = LoadSlotList(reader, 4, (r, i) => LoadSlot(r, i)),
        Chimneys = LoadSlotList(reader, 2, (r, i) => LoadSlot(r, i)),
        SmokeTraces = LoadSlotList(reader, 2, (r, i) => LoadSlot(r, i)),
        Exhausts = LoadSlotList(reader, 2, (r, i) => LoadSlot(r, i)),
        KeelTraces = LoadSlotList(reader, 2, (r, i) => LoadSlot(r, i)),
        InterfacePivot = LoadSlotList(reader, 1, (r, i) => LoadSlot(r, i)),
        CenterPivot = LoadSlotList(reader, 1, (r, i) => LoadSlot(r, i)),
        ProductionSpotStart = LoadSlotList(reader, 1, (r, i) => LoadSlot(r, i)),
        ProductionSpotEnd = LoadSlotList(reader, 1, (r, i) => LoadSlot(r, i)),
        LandingSpot = LoadSlotList(reader, 1, (r, i) => LoadSlot(r, i))
      };

    private ISlot LoadSlot(BinaryReader reader, int id)
    {
      var x = reader.ReadInt16() / 255f;
      var y = -reader.ReadInt16() / 255f;
      var z = reader.ReadInt16() / 255f;
      var result = new Slot()
      {
        Id = id,
        Position = new Vector(x, y, z),
        Direction = reader.ReadByte() / 255.0 * Math.PI * 2.0,
        Flag = reader.ReadByte()
      };
      return result;
    }

    private IMeshFrames LoadMeshFrames(BinaryReader reader)
      => new MeshFrames
      {
        BuildingFrames = reader.ReadByte(),
        ActionFrames = reader.ReadByte(),
        MovementFrames = reader.ReadByte(),
        LoopedFrames = reader.ReadByte()
      };

    private IMeshHorizontalExtents LoadMeshHorizontalExtents(BinaryReader reader)
      => new MeshHorizontalExtents
      {
        PositiveY = reader.ReadUInt16(),
        NegativeY = reader.ReadUInt16(),
        PositiveX = reader.ReadUInt16(),
        NegativeX = reader.ReadUInt16()
      };

    private IMeshFootprint LoadMeshFootprint(BinaryReader reader)
      => new MeshFootprint
      {
        BoxHeights = ReadReverseIndexed(reader, r => r.ReadUInt16()),
        BoxFlags = ReadReverseIndexed(reader, r => r.ReadByte()),
        CoverageDescriptors = LoadSlotList(reader, MeshFootprint.CoverageCount, (r, _) => r.ReadUInt32()).ToArray(),
        CoverageBitmaps = LoadSlotList(reader, MeshFootprint.CoverageCount, (r, _) => r.ReadUInt64()).ToArray()
      };

    private static T[] ReadReverseIndexed<T>(BinaryReader reader, Func<BinaryReader, T> read)
    {
      var values = new T[MeshFootprint.BoxCount];
      for (var logicalIndex = MeshFootprint.BoxCount - 1; logicalIndex >= 0; logicalIndex--)
      {
        values[logicalIndex] = read(reader);
      }

      return values;
    }

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
      return new OmniLight() { Value = GetVector(reader), Color = GetColor(reader), Radius = reader.ReadSingle() };
    }

    private Color GetColor(BinaryReader reader)
    {
      var r = reader.ReadSingle() * 255;
      var g = reader.ReadSingle() * 255;
      var b = reader.ReadSingle() * 255;
      return Color.FromArgb((int)r, (int)g, (int)b);
    }

    #endregion

    #region Geometry

    private IEnumerable<IModelPart> LoadStaticParts(BinaryReader reader, out int finalSourceDepth)
    {
      if (reader.BaseStream.Position == reader.BaseStream.Length)
      {
        throw InvalidData("StaticRenderRecord", reader.BaseStream.Position, "at least one record is required");
      }

      var parts = new List<IModelPart>();
      var sourceDepth = 0;
      while (true)
      {
        var recordOffset = reader.BaseStream.Position;
        var part = LoadPart(reader);
        try
        {
          sourceDepth = EarthMesh.AdvanceSourceDepth(sourceDepth, part);
        }
        catch (InvalidOperationException ex)
        {
          throw InvalidData("StaticRenderRecord.ObjectFlags", recordOffset,
            ex.Message.TrimEnd('.'), ex);
        }
        parts.Add(part);

        if (part.NextRecordMarker == 0)
        {
          RequireExactEnd(reader, "final zero NextRecordMarker");
          break;
        }

        if (reader.BaseStream.Position == reader.BaseStream.Length)
        {
          throw InvalidData("NextRecordMarker", reader.BaseStream.Position - sizeof(uint),
            "nonzero final marker requires another static render record");
        }
      }

      finalSourceDepth = sourceDepth;
      return parts;
    }

    private IModelPart LoadPart(BinaryReader reader)
    {
      var vertices = ReadField(reader, "Vertices", () => LoadVertices(reader)).ToArray();
      var result = new ModelPart
      {
        Vertices = vertices
      };
      EnsureRemaining(reader, sizeof(uint), "StaticRenderRecord.ObjectFlags");
      result.BackTrackDepth = reader.ReadByte();
      result.PartType = (PartType)reader.ReadByte();
      result.Empty = reader.ReadInt16();
      result.Texture = ReadField(reader, "Texture", () => LoadTextureInfo(reader));
      result.Faces = ReadField(reader, "Triangles", () => LoadFaces(reader, vertices.Length));
      result.Animations = ReadField(reader, "AnimationTracks", () => LoadAnimations(reader));
      result.AnimationType = ReadField(reader, "AnimationType", () => (AnimationType)reader.ReadInt32());
      result.Offset = ReadField(reader, "Pivot", () => LoadVector(reader));
      result.RiseAngle = ReadField(reader, "BarrelMaximumAngle",
        () =>
        {
          var serializedAngle = reader.ReadByte();
          return result.PartType.HasFlag(PartType.Barrel) ? serializedAngle * 360d / 256d : 0;
        });
      result.NextRecordMarker = ReadUInt32(reader, "StaticRenderRecord.NextRecordMarker");
      return result;
    }

    private IAnimations LoadAnimations(BinaryReader reader)
      => new Animations()
      {
        ScaleFrames = LoadSlotList(reader, reader.ReadInt32(), (r, _) => LoadVector(r)),
        TranslationFrames = LoadSlotList(reader, reader.ReadInt32(), (r, _) => LoadVector(r)),
        RotationFrames = LoadSlotList(reader, reader.ReadInt32(), (r, _) => LoadRotationFrame(r)),
      };

    private IRotationFrame LoadRotationFrame(BinaryReader reader)
      => new RotationFrame()
      {
        TransformationMatrix = new System.Numerics.Matrix4x4()
        {
          M11 = reader.ReadSingle(),
          M21 = reader.ReadSingle(),
          M31 = reader.ReadSingle(),
          M41 = reader.ReadSingle(),
          M12 = reader.ReadSingle(),
          M22 = reader.ReadSingle(),
          M32 = reader.ReadSingle(),
          M42 = reader.ReadSingle(),
          M13 = reader.ReadSingle(),
          M23 = reader.ReadSingle(),
          M33 = reader.ReadSingle(),
          M43 = reader.ReadSingle(),
          M14 = reader.ReadSingle(),
          M24 = reader.ReadSingle(),
          M34 = reader.ReadSingle(),
          M44 = reader.ReadSingle()
        }
      };

    private IEnumerable<IFace> LoadFaces(BinaryReader reader, int vertexCount)
    {
      var countOffset = reader.BaseStream.Position;
      var faceCount = reader.ReadUInt32();
      if (faceCount > int.MaxValue)
      {
        throw InvalidData("StaticRenderRecord.Triangles.Count", countOffset,
          $"unsupported count {faceCount}");
      }

      EnsureRemaining(reader, (long)faceCount * 0x08, "StaticRenderRecord.Triangles");
      var faces = new IFace[(int)faceCount];
      for (var i = 0; i < faces.Length; i++)
      {
        faces[i] = LoadFace(reader, vertexCount, i);
      }

      return faces;
    }

    private IFace LoadFace(BinaryReader reader, int vertexCount, int faceIndex)
    {
      return new Face()
      {
        V1 = ReadVertexIndex(reader, vertexCount, faceIndex, nameof(Face.V1)),
        V2 = ReadVertexIndex(reader, vertexCount, faceIndex, nameof(Face.V2)),
        V3 = ReadVertexIndex(reader, vertexCount, faceIndex, nameof(Face.V3)),
        Flags = reader.ReadUInt16(),
      };
    }

    private static ushort ReadVertexIndex(BinaryReader reader, int vertexCount, int faceIndex, string field)
    {
      var offset = reader.BaseStream.Position;
      var index = reader.ReadUInt16();
      if (index >= vertexCount)
      {
        throw InvalidData($"StaticRenderRecord.Triangles[{faceIndex}].{field}", offset,
          $"index {index} is outside the declared vertex range 0..{vertexCount - 1}");
      }

      return index;
    }

    private ITextureInfo LoadTextureInfo(BinaryReader reader)
    {
      var lengthOffset = reader.BaseStream.Position;
      var fileNameLength = reader.ReadInt32();
      if (fileNameLength < 0)
      {
        throw InvalidData("Texture.PathLength", lengthOffset, $"negative length {fileNameLength}");
      }

      var fileName = _encoding.GetString(ReadExact(reader, fileNameLength, "Texture.Path"));
      return new TextureInfo() { FileName = fileName };
    }

    private IEnumerable<IVertex> LoadVertices(BinaryReader reader)
    {
      var vertexCountOffset = reader.BaseStream.Position;
      var vertexCount = reader.ReadUInt32();
      if (vertexCount > int.MaxValue)
      {
        throw InvalidData("StaticRenderRecord.Vertices.VertexCount", vertexCountOffset,
          $"unsupported count {vertexCount}");
      }

      var blockCountOffset = reader.BaseStream.Position;
      var blockCount = reader.ReadUInt32();
      var expectedBlockCount = vertexCount / 4 + (vertexCount % 4 == 0 ? 0u : 1u);
      if (blockCount != expectedBlockCount)
      {
        throw InvalidData("StaticRenderRecord.Vertices.VertexBlockCount", blockCountOffset,
          $"declared {blockCount}, expected {expectedBlockCount} for {vertexCount} vertices");
      }

      EnsureRemaining(reader, (long)blockCount * 0xA0, "StaticRenderRecord.Vertices.VertexBlocks");
      var vertices = new List<IVertex>((int)vertexCount);
      for (var block = 0u; block < blockCount; block++)
      {
        var activeLanes = Math.Min(4, (int)vertexCount - vertices.Count);
        vertices.AddRange(GetVertices(ReadExact(reader, 0xA0,
          $"StaticRenderRecord.Vertices.VertexBlocks[{block}]")).Take(activeLanes));
      }

      return vertices;
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
        var v = BitConverter.ToSingle(vertexData, idx + 0x70);
        var w = BitConverter.ToSingle(vertexData, idx + 0x80);

        var normalVectorIdx = BitConverter.ToUInt16(vertexData, i * sizeof(ushort) + 0x90);
        var positionVectorIdx = BitConverter.ToUInt16(vertexData, i * sizeof(ushort) + 0x98);
        yield return new Vertex(new Vector(x, y, z), new Vector(normalX, normalY, normalZ),
          TextureCoordinate.FromSerialized(u, v, w), normalVectorIdx, positionVectorIdx);
      }
    }

    #endregion

    #region Common

    private IVector LoadVector(BinaryReader reader)
    {
      return new Vector() { Value = GetVector(reader), };
    }

    private System.Numerics.Vector3 GetVector(BinaryReader reader)
    {
      var x = reader.ReadSingle();
      var y = -reader.ReadSingle();
      var z = reader.ReadSingle();
      return new System.Numerics.Vector3(x, y, z);
    }

    private static uint ReadUInt32(BinaryReader reader, string field)
    {
      EnsureRemaining(reader, sizeof(uint), field);
      return reader.ReadUInt32();
    }

    private static byte[] ReadExact(BinaryReader reader, int count, string field)
    {
      EnsureRemaining(reader, count, field);
      return reader.ReadBytes(count);
    }

    private static T ReadField<T>(BinaryReader reader, string field, Func<T> read)
    {
      var offset = reader.BaseStream.Position;
      try
      {
        return read();
      }
      catch (InvalidDataException)
      {
        throw;
      }
      catch (EndOfStreamException ex)
      {
        throw InvalidData($"StaticRenderRecord.{field}", offset, "unexpected end of file", ex);
      }
      catch (ArgumentException ex)
      {
        throw InvalidData($"StaticRenderRecord.{field}", offset, ex.Message, ex);
      }
      catch (OverflowException ex)
      {
        throw InvalidData($"StaticRenderRecord.{field}", offset, ex.Message, ex);
      }
    }

    private static void EnsureRemaining(BinaryReader reader, long count, string field)
    {
      var offset = reader.BaseStream.Position;
      if (count < 0 || reader.BaseStream.Length - offset < count)
      {
        throw InvalidData(field, offset, $"requires {count} bytes");
      }
    }

    private static void RequireExactEnd(BinaryReader reader, string field)
    {
      if (reader.BaseStream.Position != reader.BaseStream.Length)
      {
        throw InvalidData(field, reader.BaseStream.Position,
          $"unexpected trailing data ({reader.BaseStream.Length - reader.BaseStream.Position} bytes)");
      }
    }

    private static InvalidDataException InvalidData(string field, long offset, string detail, Exception inner = null)
      => new InvalidDataException($"{field} at byte offset 0x{offset:X}: {detail}.", inner);

    #endregion
  }
}
