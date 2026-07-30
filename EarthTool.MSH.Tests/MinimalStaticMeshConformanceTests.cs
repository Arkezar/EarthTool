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
      Assert.Equal(16, mesh.BaseHeader.Footprint.BoxHeights.Length);
      Assert.Equal(16, mesh.BaseHeader.Footprint.BoxFlags.Length);
      Assert.Equal(4, mesh.BaseHeader.Footprint.CoverageDescriptors.Length);
      Assert.Equal(4, mesh.BaseHeader.Footprint.CoverageBitmaps.Length);
      Assert.Equal(49, CountAttachments(mesh.BaseHeader.Slots));
      Assert.Equal(1u, mesh.TrailingHierarchyUnwindCount);
      var part = Assert.Single(mesh.Geometries);
      var vertices = part.Vertices.ToArray();
      Assert.Equal(3, vertices.Length);
      Assert.Equal(new System.Numerics.Vector3(0, 0, 0), vertices[0].Position.Value);
      Assert.Equal(new System.Numerics.Vector3(1, 0, 0), vertices[1].Position.Value);
      Assert.Equal(new System.Numerics.Vector3(0, 1, 0), vertices[2].Position.Value);
      var face = Assert.Single(part.Faces);
      Assert.Equal((ushort)0, face.V1);
      Assert.Equal((ushort)1, face.V2);
      Assert.Equal((ushort)2, face.V3);
      Assert.Equal((ushort)1, face.Flags);
      Assert.Equal(0u, part.NextRecordMarker);
      Assert.Equal(fixture, File.ReadAllBytes(outputPath));
    }
    finally
    {
      File.Delete(inputPath);
      File.Delete(outputPath);
    }
  }

  [Fact]
  public void PublicReaderAndWriterPreserveAllAttachmentRecords()
  {
    const int attachmentOffset = 0x14 + 0x1D8;
    const int attachmentCount = 49;
    const int attachmentSize = 8;
    var fixture = CreateFixture();
    var headings = new byte[] { 63, 64, 65, 127, 128, 129, 191, 192, 193, 255 };
    for (var i = 0; i < attachmentCount; i++)
    {
      var offset = attachmentOffset + (i * attachmentSize);
      WriteInt16(fixture, offset, (short)(-12000 + (i * 257)));
      WriteInt16(fixture, offset + 0x02, (short)(11000 - (i * 193)));
      WriteInt16(fixture, offset + 0x04, (short)(-9000 + (i * 149)));
      fixture[offset + 0x06] = headings[i % headings.Length];
      fixture[offset + 0x07] = (byte)i;
    }

    const int unsetIndex = 32;
    var unsetOffset = attachmentOffset + (unsetIndex * attachmentSize);
    WriteInt16(fixture, unsetOffset, short.MinValue);
    WriteInt16(fixture, unsetOffset + 0x02, short.MinValue);
    WriteInt16(fixture, unsetOffset + 0x04, short.MinValue);
    fixture[unsetOffset + 0x06] = 64;
    fixture[unsetOffset + 0x07] = 0x37;
    var inputPath = GetTemporaryPath();
    var outputPath = GetTemporaryPath();

    try
    {
      File.WriteAllBytes(inputPath, fixture);

      var mesh = CreateReader().Read(inputPath);
      new EarthMeshWriter(Encoding.UTF8).Write(mesh, outputPath);

      var attachments = GetAttachments(mesh.BaseHeader.Slots).ToArray();
      Assert.Equal(0x188, mesh.BaseHeader.Slots.ToByteArray(Encoding.UTF8).Length);
      Assert.Equal(Enumerable.Range(1, attachmentCount), attachments.Select(attachment => attachment.Id));
      for (var i = 0; i < attachmentCount; i++)
      {
        var offset = attachmentOffset + (i * attachmentSize);
        var attachment = attachments[i];
        Assert.Equal(ReadInt16(fixture, offset) / 256f, attachment.Position.X);
        Assert.Equal(-ReadInt16(fixture, offset + 0x02) / 256f, attachment.Position.Y);
        Assert.Equal(ReadInt16(fixture, offset + 0x04) / 256f, attachment.Position.Z);
        Assert.Equal(fixture[offset + 0x06], attachment.Heading);
        Assert.Equal(fixture[offset + 0x07], attachment.FinalParameter);
        Assert.Equal(i != unsetIndex, attachment.IsValid);
      }

      Assert.Equal(
        fixture.AsSpan(attachmentOffset, attachmentCount * attachmentSize).ToArray(),
        File.ReadAllBytes(outputPath).AsSpan(attachmentOffset, attachmentCount * attachmentSize).ToArray());
    }
    finally
    {
      File.Delete(inputPath);
      File.Delete(outputPath);
    }
  }

  [Fact]
  public void PublicWriterTruncatesAttachmentCoordinatesAndHeadingToFormatUnits()
  {
    const int attachmentOffset = 0x14 + 0x1D8;
    var mesh = ReadValidMesh();
    var attachment = Assert.IsType<Slot>(mesh.BaseHeader.Slots.Turrets.First());
    attachment.Position = new Vector(1.003f, -2.007f, -3.999f);
    attachment.Direction = 64.999 / 256 * Math.PI * 2;
    attachment.FinalParameter = 0;
    var outputPath = GetTemporaryPath();

    try
    {
      new EarthMeshWriter(Encoding.UTF8).Write(mesh, outputPath);

      var output = File.ReadAllBytes(outputPath);
      Assert.Equal((short)256, ReadInt16(output, attachmentOffset));
      Assert.Equal((short)513, ReadInt16(output, attachmentOffset + 0x02));
      Assert.Equal((short)-1023, ReadInt16(output, attachmentOffset + 0x04));
      Assert.Equal((byte)64, output[attachmentOffset + 0x06]);
      Assert.Equal((byte)0, output[attachmentOffset + 0x07]);
    }
    finally
    {
      File.Delete(outputPath);
    }
  }

  [Fact]
  public void PublicReaderAndWriterPreserveStaticGeometryChannelsAndIgnoreUnusedLanes()
  {
    const int vertexBlockOffset = 0x388;
    var fixture = CreateFixture();
    var storedV = new[] { 0.1f, 0.7f, -0.5f };
    var textureW = new[] { 2.5f, -3.25f, 0.125f };
    var normalSharing = new ushort[] { ushort.MaxValue, 0x8001, 0x7FFF };
    var positionSharing = new ushort[] { 0xFEDC, ushort.MaxValue, 0x8000 };
    for (var lane = 0; lane < 3; lane++)
    {
      WriteSingle(fixture, vertexBlockOffset + 0x70 + (lane * sizeof(float)), storedV[lane]);
      WriteSingle(fixture, vertexBlockOffset + 0x80 + (lane * sizeof(float)), textureW[lane]);
      WriteUInt16(fixture, vertexBlockOffset + 0x90 + (lane * sizeof(ushort)), normalSharing[lane]);
      WriteUInt16(fixture, vertexBlockOffset + 0x98 + (lane * sizeof(ushort)), positionSharing[lane]);
    }

    for (var channelOffset = 0; channelOffset <= 0x80; channelOffset += 0x10)
    {
      WriteSingle(fixture, vertexBlockOffset + channelOffset + (3 * sizeof(float)), 123.5f + channelOffset);
    }
    WriteUInt16(fixture, vertexBlockOffset + 0x90 + (3 * sizeof(ushort)), 0xAAAA);
    WriteUInt16(fixture, vertexBlockOffset + 0x98 + (3 * sizeof(ushort)), 0xBBBB);
    WriteUInt16(fixture, 0x43A, 0xFEDC);

    var expectedOutput = fixture.ToArray();
    for (var channelOffset = 0; channelOffset <= 0x80; channelOffset += 0x10)
    {
      WriteSingle(expectedOutput, vertexBlockOffset + channelOffset + (3 * sizeof(float)), 0);
    }
    WriteUInt16(expectedOutput, vertexBlockOffset + 0x90 + (3 * sizeof(ushort)), 0);
    WriteUInt16(expectedOutput, vertexBlockOffset + 0x98 + (3 * sizeof(ushort)), 0);
    var inputPath = GetTemporaryPath();
    var outputPath = GetTemporaryPath();

    try
    {
      File.WriteAllBytes(inputPath, fixture);

      var mesh = CreateReader().Read(inputPath);
      new EarthMeshWriter(Encoding.UTF8).Write(mesh, outputPath);

      var part = Assert.Single(mesh.Geometries);
      var vertices = part.Vertices.ToArray();
      Assert.Equal(3, vertices.Length);
      for (var lane = 0; lane < vertices.Length; lane++)
      {
        Assert.Equal(1 - storedV[lane], vertices[lane].TextureCoordinate.V);
        Assert.Equal(textureW[lane], vertices[lane].TextureCoordinate.W);
        Assert.Equal(normalSharing[lane], vertices[lane].NormalVectorIdx);
        Assert.Equal(positionSharing[lane], vertices[lane].PositionVectorIdx);
      }

      var face = Assert.Single(part.Faces);
      Assert.Equal((ushort)0xFEDC, face.Flags);
      Assert.Equal(expectedOutput, File.ReadAllBytes(outputPath));
    }
    finally
    {
      File.Delete(inputPath);
      File.Delete(outputPath);
    }
  }

  [Fact]
  public void PublicReaderAndWriterPreserveHighUnsignedTriangleIndices()
  {
    var fixture = CreateHighTriangleIndexFixture();
    var inputPath = GetTemporaryPath();
    var outputPath = GetTemporaryPath();

    try
    {
      File.WriteAllBytes(inputPath, fixture);

      var mesh = CreateReader().Read(inputPath);
      new EarthMeshWriter(Encoding.UTF8).Write(mesh, outputPath);

      var part = Assert.Single(mesh.Geometries);
      Assert.Equal(0x8001, part.Vertices.Count());
      var face = Assert.Single(part.Faces);
      Assert.Equal((ushort)0x8000, face.V1);
      Assert.Equal((ushort)0x8001, face.Flags);
      Assert.Equal(fixture, File.ReadAllBytes(outputPath));
    }
    finally
    {
      File.Delete(inputPath);
      File.Delete(outputPath);
    }
  }

  [Fact]
  public void PublicReaderAndWriterExposeLogicalFootprintAndUnsignedExtents()
  {
    const int baseOffset = 0x14;
    var fixture = CreateFixture();
    var expectedHeights = Enumerable.Range(0, 16).Select(i => (ushort)(0x8000 + i)).ToArray();
    var expectedFlags = Enumerable.Range(0, 16).Select(i => (byte)(0xA0 + i)).ToArray();
    var expectedDescriptors = new uint[] { 0x89ABCDEF, 0x10203040, 0xFEDCBA98, 0x76543210 };
    var expectedBitmaps = new ulong[]
    {
      0x0123456789ABCDEF,
      0xFEDCBA9876543210,
      0x8000000000000001,
      0x0F1E2D3C4B5A6978
    };
    WriteUInt32(fixture, baseOffset + 0x00C, 0xF1234567);
    for (var i = 0; i < 16; i++)
    {
      WriteUInt16(fixture, baseOffset + 0x196 - (2 * i), expectedHeights[i]);
      fixture[baseOffset + 0x1A7 - i] = expectedFlags[i];
    }

    for (var i = 0; i < 4; i++)
    {
      WriteUInt32(fixture, baseOffset + 0x1A8 + (4 * i), expectedDescriptors[i]);
      WriteUInt64(fixture, baseOffset + 0x1B8 + (8 * i), expectedBitmaps[i]);
    }

    WriteUInt16(fixture, baseOffset + 0x360, 0x8001);
    WriteUInt16(fixture, baseOffset + 0x362, 0x9002);
    WriteUInt16(fixture, baseOffset + 0x364, 0xA003);
    WriteUInt16(fixture, baseOffset + 0x366, 0xB004);
    var inputPath = GetTemporaryPath();
    var outputPath = GetTemporaryPath();

    try
    {
      File.WriteAllBytes(inputPath, fixture);

      var mesh = CreateReader().Read(inputPath);
      new EarthMeshWriter(Encoding.UTF8).Write(mesh, outputPath);

      Assert.Equal(0xF1234567u, mesh.BaseHeader.BoxPresenceMask);
      Assert.Equal(expectedHeights, mesh.BaseHeader.Footprint.BoxHeights);
      Assert.Equal(expectedFlags, mesh.BaseHeader.Footprint.BoxFlags);
      Assert.Equal(expectedDescriptors, mesh.BaseHeader.Footprint.CoverageDescriptors);
      Assert.Equal(expectedBitmaps, mesh.BaseHeader.Footprint.CoverageBitmaps);
      Assert.Equal((ushort)0x8001, mesh.BaseHeader.HorizontalExtents.PositiveY);
      Assert.Equal((ushort)0x9002, mesh.BaseHeader.HorizontalExtents.NegativeY);
      Assert.Equal((ushort)0xA003, mesh.BaseHeader.HorizontalExtents.PositiveX);
      Assert.Equal((ushort)0xB004, mesh.BaseHeader.HorizontalExtents.NegativeX);
      Assert.Equal(fixture, File.ReadAllBytes(outputPath));
    }
    finally
    {
      File.Delete(inputPath);
      File.Delete(outputPath);
    }
  }

  [Fact]
  public void PublicReaderAndWriterRoundTripDocumentedCorpusFootprintExamples()
  {
    const int baseOffset = 0x14;
    var fixture = CreateFixture();
    var expectedHeights = new ushort[16];
    var expectedFlags = new byte[16];
    expectedHeights[11] = 380;
    expectedHeights[15] = 380;
    expectedHeights[10] = 493;
    expectedHeights[14] = 380;
    expectedFlags[11] = 1;
    expectedFlags[15] = 2;
    expectedFlags[10] = 8;
    expectedFlags[14] = 4;
    WriteUInt32(fixture, baseOffset + 0x00C, 0x0000CC00);
    for (var i = 0; i < 16; i++)
    {
      WriteUInt16(fixture, baseOffset + 0x196 - (2 * i), expectedHeights[i]);
      fixture[baseOffset + 0x1A7 - i] = expectedFlags[i];
    }

    WriteUInt16(fixture, baseOffset + 0x360, 43);
    WriteUInt16(fixture, baseOffset + 0x362, 43);
    WriteUInt16(fixture, baseOffset + 0x364, 49);
    WriteUInt16(fixture, baseOffset + 0x366, 49);
    var inputPath = GetTemporaryPath();
    var outputPath = GetTemporaryPath();

    try
    {
      File.WriteAllBytes(inputPath, fixture);

      var mesh = CreateReader().Read(inputPath);
      new EarthMeshWriter(Encoding.UTF8).Write(mesh, outputPath);

      Assert.Equal(0x0000CC00u, mesh.BaseHeader.BoxPresenceMask);
      Assert.Equal(expectedHeights, mesh.BaseHeader.Footprint.BoxHeights);
      Assert.Equal(expectedFlags, mesh.BaseHeader.Footprint.BoxFlags);
      Assert.Equal((ushort)43, mesh.BaseHeader.HorizontalExtents.PositiveY);
      Assert.Equal((ushort)43, mesh.BaseHeader.HorizontalExtents.NegativeY);
      Assert.Equal((ushort)49, mesh.BaseHeader.HorizontalExtents.PositiveX);
      Assert.Equal((ushort)49, mesh.BaseHeader.HorizontalExtents.NegativeX);
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
  public void PublicReaderTreatsAnyNonzeroNextRecordMarkerAsBooleanLink()
  {
    var fixture = CreateLinkedFixture(new uint[] { 0, 0 }, uint.MaxValue, 1);
    var inputPath = GetTemporaryPath();

    try
    {
      File.WriteAllBytes(inputPath, fixture);

      var mesh = CreateReader().Read(inputPath);

      var parts = mesh.Geometries.ToArray();
      Assert.Equal(2, parts.Length);
      Assert.Equal(uint.MaxValue, parts[0].NextRecordMarker);
      Assert.Equal(0u, parts[1].NextRecordMarker);
    }
    finally
    {
      File.Delete(inputPath);
    }
  }

  [Fact]
  public void PublicReaderAndWriterRoundTripBarrelMaximumAngle()
  {
    const int objectFlagsOffset = 0x428;
    const int barrelMaximumAngleOffset = 0x458;
    var fixture = CreateFixture();
    WriteUInt32(fixture, objectFlagsOffset, (uint)EarthTool.MSH.Enums.PartType.Barrel << 8);
    fixture[barrelMaximumAngleOffset] = 14;
    var inputPath = GetTemporaryPath();
    var outputPath = GetTemporaryPath();

    try
    {
      File.WriteAllBytes(inputPath, fixture);

      var mesh = CreateReader().Read(inputPath);
      new EarthMeshWriter(Encoding.UTF8).Write(mesh, outputPath);

      Assert.Equal(19.6875, Assert.Single(mesh.Geometries).RiseAngle);
      Assert.Equal(14, File.ReadAllBytes(outputPath)[barrelMaximumAngleOffset]);
    }
    finally
    {
      File.Delete(inputPath);
      File.Delete(outputPath);
    }
  }

  [Fact]
  public void PublicWriterTruncatesBarrelMaximumAngleToOneByteTurnUnits()
  {
    const int barrelMaximumAngleOffset = 0x458;
    var mesh = ReadValidMesh();
    var part = Assert.IsType<ModelPart>(Assert.Single(mesh.Geometries));
    part.PartType = EarthTool.MSH.Enums.PartType.Barrel;
    part.RiseAngle = 19.9;
    var outputPath = GetTemporaryPath();

    try
    {
      new EarthMeshWriter(Encoding.UTF8).Write(mesh, outputPath);

      Assert.Equal(14, File.ReadAllBytes(outputPath)[barrelMaximumAngleOffset]);
    }
    finally
    {
      File.Delete(outputPath);
    }
  }

  [Fact]
  public void PublicReaderAndWriterClearBarrelMaximumAngleForNonBarrelRecord()
  {
    const int barrelMaximumAngleOffset = 0x458;
    var fixture = CreateFixture();
    fixture[barrelMaximumAngleOffset] = 14;
    var inputPath = GetTemporaryPath();
    var outputPath = GetTemporaryPath();

    try
    {
      File.WriteAllBytes(inputPath, fixture);

      var mesh = CreateReader().Read(inputPath);
      new EarthMeshWriter(Encoding.UTF8).Write(mesh, outputPath);

      var part = Assert.Single(mesh.Geometries);
      Assert.False(part.PartType.HasFlag(EarthTool.MSH.Enums.PartType.Barrel));
      Assert.Equal(0, part.RiseAngle);
      Assert.Equal(0, File.ReadAllBytes(outputPath)[barrelMaximumAngleOffset]);
    }
    finally
    {
      File.Delete(inputPath);
      File.Delete(outputPath);
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
  [InlineData("BoxHeights")]
  [InlineData("BoxFlags")]
  [InlineData("CoverageDescriptors")]
  [InlineData("CoverageBitmaps")]
  [InlineData("Slots.Turrets")]
  public void PublicWriterRejectsInvalidFixedBaseHeaderCollections(string field)
  {
    var mesh = ReadValidMesh();
    var baseHeader = Assert.IsType<MeshBaseHeader>(mesh.BaseHeader);
    var footprint = Assert.IsType<MeshFootprint>(baseHeader.Footprint);
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
      case "BoxHeights":
        footprint.BoxHeights = new ushort[15];
        break;
      case "BoxFlags":
        footprint.BoxFlags = new byte[15];
        break;
      case "CoverageDescriptors":
        footprint.CoverageDescriptors = new uint[3];
        break;
      case "CoverageBitmaps":
        footprint.CoverageBitmaps = new ulong[3];
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
  public void PublicWriterCanonicalizesRecordMarkersFromSequencePosition()
  {
    var inputPath = GetTemporaryPath();
    var outputPath = GetTemporaryPath();

    try
    {
      File.WriteAllBytes(inputPath, CreateLinkedFixture(new uint[] { 0, 0 }, uint.MaxValue, 1));
      var mesh = CreateReader().Read(inputPath);
      var parts = mesh.Geometries.Cast<ModelPart>().ToArray();
      parts[0].NextRecordMarker = 0;
      parts[1].NextRecordMarker = uint.MaxValue;

      new EarthMeshWriter(Encoding.UTF8).Write(mesh, outputPath);

      var output = File.ReadAllBytes(outputPath);
      Assert.Equal(1u, BinaryPrimitives.ReadUInt32LittleEndian(output.AsSpan(0x459)));
      Assert.Equal(0u, BinaryPrimitives.ReadUInt32LittleEndian(output.AsSpan(0x536)));
    }
    finally
    {
      File.Delete(inputPath);
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

    var tooFewVertexBlocks = CreateFixture();
    WriteUInt32(tooFewVertexBlocks, 0x380, 5);
    yield return new object[] { tooFewVertexBlocks, "VertexBlockCount" };

    var tooManyVertexBlocks = CreateFixture();
    WriteUInt32(tooManyVertexBlocks, 0x384, 2);
    yield return new object[] { tooManyVertexBlocks, "VertexBlockCount" };

    var truncatedTexture = CreateFixture();
    WriteUInt32(truncatedTexture, 0x42C, uint.MaxValue);
    yield return new object[] { truncatedTexture, "Texture.PathLength" };

    var invalidTriangleIndex = CreateFixture();
    WriteUInt16(invalidTriangleIndex, 0x438, 3);
    yield return new object[] { invalidTriangleIndex, "Triangles[0].V3" };

    var hierarchyUnderflow = CreateFixture();
    WriteUInt32(hierarchyUnderflow, 0x428, 1);
    yield return new object[] { hierarchyUnderflow, "hierarchy underflow" };

    var unwindMismatch = CreateFixture();
    WriteUInt32(unwindMismatch, 0x37C, 2);
    yield return new object[] { unwindMismatch, "TrailingHierarchyUnwindCount" };

    var nonzeroFinalMarker = CreateFixture();
    WriteUInt32(nonzeroFinalMarker, nonzeroFinalMarker.Length - sizeof(uint), 1);
    yield return new object[] { nonzeroFinalMarker, "NextRecordMarker" };

    var zeroBeforeEnd = CreateLinkedFixture(new uint[] { 0, 0 }, 1, 1);
    WriteUInt32(zeroBeforeEnd, 0x459, 0);
    yield return new object[] { zeroBeforeEnd, "trailing data" };

    var truncatedLinkedRecord = CreateLinkedFixture(new uint[] { 0, 0 }, 1, 1)[..0x470];
    yield return new object[] { truncatedLinkedRecord, "Vertices" };

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

  private static byte[] CreateHighTriangleIndexFixture()
  {
    const int recordOffset = 0x380;
    const int vertexDataOffset = recordOffset + 0x08;
    const int vertexBlockSize = 0xA0;
    const int vertexCount = 0x8001;
    const int vertexBlockCount = (vertexCount + 3) / 4;
    var canonical = CreateFixture();
    var additionalVertexBytes = (vertexBlockCount - 1) * vertexBlockSize;
    var data = new byte[canonical.Length + additionalVertexBytes];
    canonical.AsSpan(0, vertexDataOffset + vertexBlockSize).CopyTo(data);
    canonical.AsSpan(vertexDataOffset + vertexBlockSize)
      .CopyTo(data.AsSpan(vertexDataOffset + (vertexBlockCount * vertexBlockSize)));
    WriteUInt32(data, recordOffset, vertexCount);
    WriteUInt32(data, recordOffset + sizeof(uint), vertexBlockCount);

    var triangleOffset = vertexDataOffset + (vertexBlockCount * vertexBlockSize) + 0x0C;
    WriteUInt16(data, triangleOffset, 0x8000);
    WriteUInt16(data, triangleOffset + 0x02, 0);
    WriteUInt16(data, triangleOffset + 0x04, 1);
    WriteUInt16(data, triangleOffset + 0x06, 0x8001);
    return data;
  }

  private static byte[] CreateLinkedFixture(uint[] objectFlags, uint nonfinalMarker, uint trailingUnwind)
  {
    const int recordsOffset = 0x380;
    const int recordSize = 0xDD;
    const int objectFlagsOffset = 0xA8;
    const int nextRecordMarkerOffset = 0xD9;
    var canonical = CreateFixture();
    var record = canonical.AsSpan(recordsOffset, recordSize).ToArray();
    var data = new byte[recordsOffset + (recordSize * objectFlags.Length)];
    canonical.AsSpan(0, recordsOffset).CopyTo(data);
    WriteUInt32(data, 0x37C, trailingUnwind);

    for (var i = 0; i < objectFlags.Length; i++)
    {
      var offset = recordsOffset + (recordSize * i);
      record.CopyTo(data, offset);
      WriteUInt32(data, offset + objectFlagsOffset, objectFlags[i]);
      WriteUInt32(data, offset + nextRecordMarkerOffset,
        i == objectFlags.Length - 1 ? 0 : nonfinalMarker);
    }

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
    => GetAttachments(slots).Count();

  private static IEnumerable<ISlot> GetAttachments(IModelSlots slots)
    => slots.Turrets.Concat(slots.BarrelMuzzels)
      .Concat(slots.TurretMuzzels)
      .Concat(slots.Headlights)
      .Concat(slots.Omnilights)
      .Concat(slots.UnloadPoints)
      .Concat(slots.HitSpots)
      .Concat(slots.SmokeSpots)
      .Concat(slots.Unknown)
      .Concat(slots.Chimneys)
      .Concat(slots.SmokeTraces)
      .Concat(slots.Exhausts)
      .Concat(slots.KeelTraces)
      .Concat(slots.InterfacePivot)
      .Concat(slots.CenterPivot)
      .Concat(slots.ProductionSpotStart)
      .Concat(slots.ProductionSpotEnd)
      .Concat(slots.LandingSpot);

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

  private static void WriteInt16(byte[] data, int offset, short value)
    => BinaryPrimitives.WriteInt16LittleEndian(data.AsSpan(offset), value);

  private static short ReadInt16(byte[] data, int offset)
    => BinaryPrimitives.ReadInt16LittleEndian(data.AsSpan(offset));

  private static void WriteUInt32(byte[] data, int offset, uint value)
    => BinaryPrimitives.WriteUInt32LittleEndian(data.AsSpan(offset), value);

  private static void WriteUInt64(byte[] data, int offset, ulong value)
    => BinaryPrimitives.WriteUInt64LittleEndian(data.AsSpan(offset), value);

  private static void WriteSingle(byte[] data, int offset, float value)
    => WriteUInt32(data, offset, BitConverter.SingleToUInt32Bits(value));
}
