using EarthTool.Common.Enums;
using EarthTool.Common.Factories;
using EarthTool.MSH.Interfaces;
using EarthTool.MSH.Models;
using EarthTool.MSH.Models.Collections;
using EarthTool.MSH.Models.Elements;
using EarthTool.MSH.Services;
using System.Buffers.Binary;
using System.Text;

namespace EarthTool.MSH.Tests;

public class MinimalStaticMeshConformanceTests
{
  private static readonly Guid FixtureGuid = new("12345678-9abc-def0-1234-56789abcdef0");

  [Fact]
  public void PublicReaderAndWriterRoundTripCanonicalOneTriangleFileByteExactly()
  {
    var inputPath = GetTemporaryPath();
    var outputPath = GetTemporaryPath();
    var fixture = CreateFixture();

    try
    {
      File.WriteAllBytes(inputPath, fixture);
      var reader = new EarthMeshReader(new EarthInfoFactory(Encoding.UTF8), new HierarchyBuilder(), Encoding.UTF8);
      var writer = new EarthMeshWriter(Encoding.UTF8);

      var mesh = reader.Read(inputPath);
      writer.Write(mesh, outputPath);

      Assert.Equal(FixtureGuid, mesh.FileHeader.Guid);
      Assert.Equal(MeshKind.Static, mesh.BaseHeader.MeshKind);
      Assert.Equal(MeshBaseHeader.SupportedVersion, 1u);
      Assert.Equal(MeshBaseHeader.SerializedSize, mesh.BaseHeader.ToByteArray(Encoding.UTF8).Length);
      Assert.Equal(fixture[0x14..0x37C], mesh.BaseHeader.ToByteArray(Encoding.UTF8));
      Assert.Equal(4, mesh.BaseHeader.MountPoints.Count());
      Assert.Equal(4, mesh.BaseHeader.SpotLights.Count());
      Assert.Equal(4, mesh.BaseHeader.OmnidirectionalLights.Count());
      Assert.Equal(16, mesh.BaseHeader.TemplateDetails.SectionHeights.Length);
      Assert.Equal(16, mesh.BaseHeader.TemplateDetails.SectionFlags.Length);
      Assert.Equal(4, mesh.BaseHeader.TemplateDetails.SectionRotations.Count());
      Assert.Equal(4, mesh.BaseHeader.TemplateDetails.SectionFlagRotations.Count());
      Assert.Equal(49, CountAttachments(mesh.BaseHeader.Slots));
      Assert.Equal(1u, mesh.TrailingHierarchyUnwindCount);
      var part = Assert.Single(mesh.Geometries);
      var vertices = part.Vertices.ToArray();
      Assert.Equal(3, vertices.Length);
      Assert.Equal(new System.Numerics.Vector3(0, 0, 0), vertices[0].Position.Value);
      Assert.Equal(new System.Numerics.Vector3(1, 0, 0), vertices[1].Position.Value);
      Assert.Equal(new System.Numerics.Vector3(0, 1, 0), vertices[2].Position.Value);
      var face = Assert.Single(part.Faces);
      Assert.Equal((short)0, face.V1);
      Assert.Equal((short)1, face.V2);
      Assert.Equal((short)2, face.V3);
      Assert.Equal((short)1, face.UNKNOWN);
      Assert.Equal(0u, part.NextRecordMarker);
      Assert.Equal(fixture, File.ReadAllBytes(outputPath));
    }
    finally
    {
      File.Delete(inputPath);
      File.Delete(outputPath);
    }
  }

  [Theory]
  [MemberData(nameof(MalformedFixtures))]
  public void PublicReaderRejectsMalformedStaticFiles(byte[] fixture, string expectedField)
  {
    var inputPath = GetTemporaryPath();
    try
    {
      File.WriteAllBytes(inputPath, fixture);
      var reader = CreateReader();

      var exception = Assert.Throws<InvalidDataException>(() => reader.Read(inputPath));

      Assert.Contains(expectedField, exception.Message, StringComparison.OrdinalIgnoreCase);
      Assert.Contains("offset", exception.Message, StringComparison.OrdinalIgnoreCase);
    }
    finally
    {
      File.Delete(inputPath);
    }
  }

  [Fact]
  public void PublicReaderAcceptsCompleteEmptyGuid()
  {
    var fixture = CreateFixture();
    Array.Clear(fixture, 0x04, 16);
    var inputPath = GetTemporaryPath();

    try
    {
      File.WriteAllBytes(inputPath, fixture);

      var mesh = CreateReader().Read(inputPath);

      Assert.Equal(Guid.Empty, mesh.FileHeader.Guid);
    }
    finally
    {
      File.Delete(inputPath);
    }
  }

  [Fact]
  public void NewlyAuthoredStaticMeshPreservesGeneratedGuidAcrossWrites()
  {
    var fixturePath = GetTemporaryPath();
    var firstOutputPath = GetTemporaryPath();
    var secondOutputPath = GetTemporaryPath();

    try
    {
      File.WriteAllBytes(fixturePath, CreateFixture());
      var parsed = CreateReader().Read(fixturePath);
      var authored = new EarthMesh
      {
        BaseHeader = parsed.BaseHeader,
        Geometries = parsed.Geometries,
        PartsTree = parsed.PartsTree
      };
      var generatedGuid = authored.FileHeader.Guid;
      var writer = new EarthMeshWriter(Encoding.UTF8);

      writer.Write(authored, firstOutputPath);
      writer.Write(authored, secondOutputPath);

      Assert.NotNull(generatedGuid);
      Assert.NotEqual(Guid.Empty, generatedGuid);
      Assert.Equal(generatedGuid, authored.FileHeader.Guid);
      Assert.Equal(File.ReadAllBytes(firstOutputPath), File.ReadAllBytes(secondOutputPath));
    }
    finally
    {
      File.Delete(fixturePath);
      File.Delete(firstOutputPath);
      File.Delete(secondOutputPath);
    }
  }

  [Theory]
  [InlineData("MountPoints")]
  [InlineData("SpotLights")]
  [InlineData("OmnidirectionalLights")]
  [InlineData("SectionHeights")]
  [InlineData("SectionFlags")]
  [InlineData("SectionRotations")]
  [InlineData("SectionFlagRotations")]
  [InlineData("Slots.Turrets")]
  public void PublicWriterRejectsInvalidFixedBaseHeaderCollections(string field)
  {
    var mesh = ReadValidMesh();
    var baseHeader = Assert.IsType<MeshBaseHeader>(mesh.BaseHeader);
    var templateDetails = Assert.IsType<TemplateDetails>(baseHeader.TemplateDetails);
    var slots = Assert.IsType<ModelSlots>(baseHeader.Slots);
    switch (field)
    {
      case "MountPoints":
        baseHeader.MountPoints = baseHeader.MountPoints.Take(3);
        break;
      case "SpotLights":
        baseHeader.SpotLights = baseHeader.SpotLights.Take(3);
        break;
      case "OmnidirectionalLights":
        baseHeader.OmnidirectionalLights = baseHeader.OmnidirectionalLights.Take(3);
        break;
      case "SectionHeights":
        templateDetails.SectionHeights = new short[3, 4];
        break;
      case "SectionFlags":
        templateDetails.SectionFlags = new byte[4, 3];
        break;
      case "SectionRotations":
        templateDetails.SectionRotations = templateDetails.SectionRotations.Take(3);
        break;
      case "SectionFlagRotations":
        templateDetails.SectionFlagRotations = templateDetails.SectionFlagRotations.Take(3);
        break;
      case "Slots.Turrets":
        slots.Turrets = slots.Turrets.Take(3);
        break;
    }

    var outputPath = GetTemporaryPath();
    try
    {
      var exception = Assert.Throws<InvalidOperationException>(() => new EarthMeshWriter(Encoding.UTF8).Write(mesh, outputPath));

      Assert.Contains(field, exception.Message, StringComparison.Ordinal);
    }
    finally
    {
      File.Delete(outputPath);
    }
  }

  [Fact]
  public void PublicWriterRejectsNullFixedBaseHeaderRecord()
  {
    var mesh = ReadValidMesh();
    var baseHeader = Assert.IsType<MeshBaseHeader>(mesh.BaseHeader);
    baseHeader.MountPoints = baseHeader.MountPoints.Take(3).Append(null!);
    var outputPath = GetTemporaryPath();

    try
    {
      var exception = Assert.Throws<InvalidOperationException>(() => new EarthMeshWriter(Encoding.UTF8).Write(mesh, outputPath));

      Assert.Contains("MountPoints cannot contain null records", exception.Message, StringComparison.Ordinal);
    }
    finally
    {
      File.Delete(outputPath);
    }
  }

  [Fact]
  public void PublicWriterRejectsStaticMeshWithoutRenderRecords()
  {
    var mesh = ReadValidMesh();
    mesh.Geometries = Array.Empty<IModelPart>();
    var outputPath = GetTemporaryPath();

    try
    {
      var exception = Assert.Throws<InvalidOperationException>(() => new EarthMeshWriter(Encoding.UTF8).Write(mesh, outputPath));

      Assert.Contains("at least one render record", exception.Message, StringComparison.OrdinalIgnoreCase);
    }
    finally
    {
      File.Delete(outputPath);
    }
  }

  [Fact]
  public void PublicWriterRejectsNullStaticRenderRecord()
  {
    var mesh = ReadValidMesh();
    mesh.Geometries = new IModelPart[] { null! };
    var outputPath = GetTemporaryPath();

    try
    {
      var exception = Assert.Throws<InvalidOperationException>(() => new EarthMeshWriter(Encoding.UTF8).Write(mesh, outputPath));

      Assert.Contains("cannot contain null records", exception.Message, StringComparison.Ordinal);
    }
    finally
    {
      File.Delete(outputPath);
    }
  }

  [Fact]
  public void PublicWriterRejectsNonzeroFinalRecordMarker()
  {
    var mesh = ReadValidMesh();
    Assert.IsType<ModelPart>(Assert.Single(mesh.Geometries)).NextRecordMarker = 1;
    var outputPath = GetTemporaryPath();

    try
    {
      var exception = Assert.Throws<InvalidOperationException>(() => new EarthMeshWriter(Encoding.UTF8).Write(mesh, outputPath));

      Assert.Contains("Final static NextRecordMarker", exception.Message, StringComparison.Ordinal);
    }
    finally
    {
      File.Delete(outputPath);
    }
  }

  [Theory]
  [InlineData(FileFlags.Resource)]
  [InlineData(FileFlags.Named)]
  public void PublicWriterRejectsHiddenStaticArchiveFields(FileFlags hiddenField)
  {
    var mesh = ReadValidMesh();
    var factory = new EarthInfoFactory(Encoding.UTF8);
    mesh.FileHeader = hiddenField == FileFlags.Resource
      ? factory.Get(FileFlags.Guid, FixtureGuid, ResourceType.Effect)
      : factory.Get(FileFlags.Guid, FixtureGuid, translationId: "hidden");
    mesh.FileHeader.RemoveFlag(hiddenField);
    var outputPath = GetTemporaryPath();

    try
    {
      var exception = Assert.Throws<InvalidOperationException>(() => new EarthMeshWriter(Encoding.UTF8).Write(mesh, outputPath));

      Assert.Contains("exactly the GUID archive field", exception.Message, StringComparison.Ordinal);
    }
    finally
    {
      File.Delete(outputPath);
    }
  }

  [Fact]
  public void PublicWriterUsesBareMeshFramingForNestedDynamicRecord()
  {
    var parsed = ReadValidMesh();
    var baseHeader = Assert.IsType<MeshBaseHeader>(parsed.BaseHeader);
    baseHeader.MeshKind = MeshKind.Dynamic;
    var child = new EarthMesh
    {
      BaseHeader = baseHeader,
      RootDynamic = CreateDynamicPart(Array.Empty<IMesh>())
    };
    var mesh = new EarthMesh
    {
      FileHeader = new EarthInfoFactory(Encoding.UTF8).Get(
        FileFlags.Resource | FileFlags.Guid, Guid.Empty, ResourceType.Effect),
      BaseHeader = baseHeader,
      RootDynamic = CreateDynamicPart(new[] { child })
    };
    var outputPath = GetTemporaryPath();

    try
    {
      new EarthMeshWriter(Encoding.UTF8).Write(mesh, outputPath);

      var output = File.ReadAllBytes(outputPath);
      Assert.Equal(0x30D0A1FFu, BinaryPrimitives.ReadUInt32LittleEndian(output));
      Assert.Equal(2, CountSequence(output, "MESH"u8));
    }
    finally
    {
      File.Delete(outputPath);
    }
  }

  public static IEnumerable<object[]> MalformedFixtures()
  {
    var bareMesh = CreateFixture()[0x14..];
    yield return new object[] { bareMesh, "ArchiveFraming" };

    var truncatedGuid = CreateFixture()[..0x0A];
    yield return new object[] { truncatedGuid, "ArchiveGuid" };

    var wrongFraming = CreateFixture();
    wrongFraming[0] = 0;
    yield return new object[] { wrongFraming, "ArchiveFraming" };

    var mismatchedDynamicFraming = CreateFixture();
    WriteUInt32(mismatchedDynamicFraming, 0, 0x30D0A1FF);
    yield return new object[] { mismatchedDynamicFraming, "ArchiveResourceType" };

    var wrongVersion = CreateFixture();
    WriteUInt32(wrongVersion, 0x18, 2);
    yield return new object[] { wrongVersion, "Version" };

    var wrongMagic = CreateFixture();
    wrongMagic[0x14] = 0;
    yield return new object[] { wrongMagic, "Magic" };

    var wrongKind = CreateFixture();
    WriteUInt32(wrongKind, 0x1C, 1);
    yield return new object[] { wrongKind, "MeshKind" };

    var noRenderRecords = CreateFixture()[..0x380];
    yield return new object[] { noRenderRecords, "StaticRenderRecord" };

    var truncatedVertices = CreateFixture()[..0x390];
    yield return new object[] { truncatedVertices, "Vertices" };

    var truncatedTexture = CreateFixture();
    WriteUInt32(truncatedTexture, 0x42C, uint.MaxValue);
    yield return new object[] { truncatedTexture, "Texture.PathLength" };

    var hierarchyUnderflow = CreateFixture();
    WriteUInt32(hierarchyUnderflow, 0x428, 1);
    yield return new object[] { hierarchyUnderflow, "hierarchy underflow" };

    var unwindMismatch = CreateFixture();
    WriteUInt32(unwindMismatch, 0x37C, 2);
    yield return new object[] { unwindMismatch, "TrailingHierarchyUnwindCount" };

    var nonzeroFinalMarker = CreateFixture();
    WriteUInt32(nonzeroFinalMarker, nonzeroFinalMarker.Length - sizeof(uint), 1);
    yield return new object[] { nonzeroFinalMarker, "NextRecordMarker" };

    var trailingData = CreateFixture().Concat(new byte[] { 0xFF }).ToArray();
    yield return new object[] { trailingData, "trailing data" };
  }

  private static byte[] CreateFixture()
  {
    const int archiveHeaderSize = 0x14;
    const int baseHeaderSize = 0x368;
    const int staticRecordSize = 0xDD;
    var data = new byte[archiveHeaderSize + baseHeaderSize + sizeof(uint) + staticRecordSize];

    WriteUInt32(data, 0x00, 0x20D0A1FF);
    FixtureGuid.ToByteArray().CopyTo(data, 0x04);

    var baseOffset = archiveHeaderSize;
    "MESH"u8.CopyTo(data.AsSpan(baseOffset));
    WriteUInt32(data, baseOffset + 0x04, 1);
    WriteUInt32(data, baseOffset + 0x08, 0);

    WriteUInt32(data, 0x37C, 1);

    var recordOffset = 0x380;
    WriteUInt32(data, recordOffset, 3);
    WriteUInt32(data, recordOffset + 0x04, 1);
    WriteSingle(data, recordOffset + 0x08 + 0x04, 1);
    WriteSingle(data, recordOffset + 0x08 + 0x10 + 0x08, -1);
    for (var lane = 0; lane < 3; lane++)
    {
      WriteSingle(data, recordOffset + 0x08 + 0x70 + (lane * sizeof(float)), 0.5f);
      WriteUInt16(data, recordOffset + 0x08 + 0x90 + (lane * sizeof(ushort)), ushort.MaxValue);
      WriteUInt16(data, recordOffset + 0x08 + 0x98 + (lane * sizeof(ushort)), ushort.MaxValue);
    }

    var cursor = recordOffset + 0x08 + 0xA0;
    WriteUInt32(data, cursor, 0);
    cursor += sizeof(uint);
    WriteUInt32(data, cursor, 0);
    cursor += sizeof(uint);
    WriteUInt32(data, cursor, 1);
    cursor += sizeof(uint);
    WriteUInt16(data, cursor, 0);
    WriteUInt16(data, cursor + 0x02, 1);
    WriteUInt16(data, cursor + 0x04, 2);
    WriteUInt16(data, cursor + 0x06, 1);
    cursor += 0x08;
    WriteUInt32(data, cursor, 0);
    WriteUInt32(data, cursor + 0x04, 0);
    WriteUInt32(data, cursor + 0x08, 0);
    cursor += 0x0C;
    WriteUInt32(data, cursor, 0);
    cursor += sizeof(uint) + 0x0C;
    data[cursor] = 0;
    cursor++;
    WriteUInt32(data, cursor, 0);

    return data;
  }

  private static string GetTemporaryPath()
    => Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid():N}.msh");

  private static EarthMeshReader CreateReader()
    => new(new EarthInfoFactory(Encoding.UTF8), new HierarchyBuilder(), Encoding.UTF8);

  private static EarthMesh ReadValidMesh()
  {
    var path = GetTemporaryPath();
    try
    {
      File.WriteAllBytes(path, CreateFixture());
      return Assert.IsType<EarthMesh>(CreateReader().Read(path));
    }
    finally
    {
      File.Delete(path);
    }
  }

  private static int CountAttachments(IModelSlots slots)
    => slots.Turrets.Count() + slots.BarrelMuzzels.Count() + slots.TurretMuzzels.Count() +
       slots.Headlights.Count() + slots.Omnilights.Count() + slots.UnloadPoints.Count() +
       slots.HitSpots.Count() + slots.SmokeSpots.Count() + slots.Unknown.Count() +
       slots.Chimneys.Count() + slots.SmokeTraces.Count() + slots.Exhausts.Count() +
       slots.KeelTraces.Count() + slots.InterfacePivot.Count() + slots.CenterPivot.Count() +
       slots.ProductionSpotStart.Count() + slots.ProductionSpotEnd.Count() + slots.LandingSpot.Count();

  private static DynamicPart CreateDynamicPart(IEnumerable<IMesh> subMeshes)
    => new()
    {
      Size1 = new EarthTool.MSH.Models.Size(),
      Size2 = new EarthTool.MSH.Models.Size(),
      Model = new TextureInfo { FileName = string.Empty },
      Texture = new TextureInfo { FileName = string.Empty },
      SubMeshes = subMeshes
    };

  private static int CountSequence(byte[] data, ReadOnlySpan<byte> sequence)
  {
    var count = 0;
    for (var offset = 0; offset <= data.Length - sequence.Length; offset++)
    {
      if (data.AsSpan(offset, sequence.Length).SequenceEqual(sequence))
      {
        count++;
      }
    }

    return count;
  }

  private static void WriteUInt16(byte[] data, int offset, ushort value)
    => BinaryPrimitives.WriteUInt16LittleEndian(data.AsSpan(offset), value);

  private static void WriteUInt32(byte[] data, int offset, uint value)
    => BinaryPrimitives.WriteUInt32LittleEndian(data.AsSpan(offset), value);

  private static void WriteSingle(byte[] data, int offset, float value)
    => WriteUInt32(data, offset, BitConverter.SingleToUInt32Bits(value));
}
