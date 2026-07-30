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
        LightVector = GetVector3(reader),
        ColorRgb = GetVector3(reader),
        ColorParameter = reader.ReadSingle(),
        AlphaInt = reader.ReadInt32(),
        AlphaB = reader.ReadSingle(),
        AlphaA = reader.ReadSingle(),
        Scale = new Vector2(reader.ReadSingle(), reader.ReadSingle()),
        Position1 = GetVector(reader),
        Position2 = GetVector(reader),
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
        SpotLights = LoadSlotList(reader, 4, (r, _) => LoadSpotLight(r)).ToArray(),
        OmnidirectionalLights = LoadSlotList(reader, 4, (r, _) => LoadOmniLight(r)).ToArray(),
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
        Turrets = LoadAttachments(reader, 4, 1),
        BarrelMuzzels = LoadAttachments(reader, 4, 5),
        TurretMuzzels = LoadAttachments(reader, 4, 9),
        Headlights = LoadAttachments(reader, 4, 13),
        Omnilights = LoadAttachments(reader, 4, 17),
        UnloadPoints = LoadAttachments(reader, 4, 21),
        HitSpots = LoadAttachments(reader, 4, 25),
        SmokeSpots = LoadAttachments(reader, 4, 29),
        Unknown = LoadAttachments(reader, 4, 33),
        Chimneys = LoadAttachments(reader, 2, 37),
        SmokeTraces = LoadAttachments(reader, 2, 39),
        Exhausts = LoadAttachments(reader, 2, 41),
        KeelTraces = LoadAttachments(reader, 2, 43),
        InterfacePivot = LoadAttachments(reader, 1, 45),
        CenterPivot = LoadAttachments(reader, 1, 46),
        ProductionSpotStart = LoadAttachments(reader, 1, 47),
        ProductionSpotEnd = LoadAttachments(reader, 1, 48),
        LandingSpot = LoadAttachments(reader, 1, 49)
      };

    private IEnumerable<ISlot> LoadAttachments(BinaryReader reader, int count, int firstId)
      => LoadSlotList(reader, count, (r, i) => LoadSlot(r, firstId + i));

    private ISlot LoadSlot(BinaryReader reader, int id)
    {
      var x = reader.ReadInt16() / 256f;
      var y = -reader.ReadInt16() / 256f;
      var z = reader.ReadInt16() / 256f;
      var result = new Slot()
      {
        Id = id,
        Position = new Vector(x, y, z),
        Heading = reader.ReadByte(),
        ExtraAngle = reader.ReadByte()
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
        Position = GetVector(reader),
        LightParameters = GetVector3(reader),
        HorizontalTargetDistance = reader.ReadSingle(),
        TargetHeading = reader.ReadByte(),
        Reserved1 = reader.ReadByte(),
        Reserved2 = reader.ReadByte(),
        Reserved3 = reader.ReadByte(),
        ConeHalfAngleTangent = reader.ReadSingle(),
        DistanceScaledCone = reader.ReadSingle(),
        VerticalTargetSlope = reader.ReadSingle(),
        FinalParameter = reader.ReadSingle()
      };
    }

    private IOmniLight LoadOmniLight(BinaryReader reader)
    {
      return new OmniLight()
      {
        Position = GetVector(reader),
        LightParameters = GetVector3(reader),
        FinalParameter = reader.ReadSingle()
      };
    }

    private static Vector3 GetVector3(BinaryReader reader)
      => new Vector3(reader.ReadSingle(), reader.ReadSingle(), reader.ReadSingle());

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
        ScaleFrames = LoadSlotList(reader, reader.ReadInt32(), (r, _) => LoadScaleVector(r)),
        TranslationFrames = LoadSlotList(reader, reader.ReadInt32(), (r, _) => LoadVector(r)),
        RotationFrames = LoadSlotList(reader, reader.ReadInt32(), (r, _) => LoadRotationFrame(r)),
      };

    private IVector LoadScaleVector(BinaryReader reader)
    {
      return new Vector(reader.ReadSingle(), reader.ReadSingle(), reader.ReadSingle());
    }

    private IRotationFrame LoadRotationFrame(BinaryReader reader)
      => new RotationFrame()
      {
        TransformationMatrix = new System.Numerics.Matrix4x4(
          reader.ReadSingle(), reader.ReadSingle(), reader.ReadSingle(), reader.ReadSingle(),
          reader.ReadSingle(), reader.ReadSingle(), reader.ReadSingle(), reader.ReadSingle(),
          reader.ReadSingle(), reader.ReadSingle(), reader.ReadSingle(), reader.ReadSingle(),
          reader.ReadSingle(), reader.ReadSingle(), reader.ReadSingle(), reader.ReadSingle())
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
