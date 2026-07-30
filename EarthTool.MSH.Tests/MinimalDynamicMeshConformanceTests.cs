using EarthTool.Common.Factories;
using EarthTool.MSH.Interfaces;
using EarthTool.MSH.Models;
using EarthTool.MSH.Services;
using System.Buffers.Binary;
using System.Collections;
using System.Numerics;
using System.Text;

namespace EarthTool.MSH.Tests;

public class MinimalDynamicMeshConformanceTests
{
  private const int ArchiveHeaderSize = 0x18;
  private const int DynamicRecordSize = 0x410;
  private static readonly Guid FixtureGuid = new("12345678-9abc-def0-1234-56789abcdef0");

  [Fact]
  public void PublicWriterSerializesDocumentedChildlessDynamicDefaults()
  {
    var fixture = CreateDefaultFixture();
    var mesh = ReadFixture(fixture);
    mesh.RootDynamic = new DynamicPart();
    var outputPath = GetTemporaryPath();

    try
    {
      new EarthMeshWriter(Encoding.UTF8).Write(mesh, outputPath);

      var output = File.ReadAllBytes(outputPath);
      Assert.Equal(ArchiveHeaderSize + DynamicRecordSize, output.Length);
      Assert.Equal(0x30D0A1FFu, BinaryPrimitives.ReadUInt32LittleEndian(output));
      Assert.Equal(1u, BinaryPrimitives.ReadUInt32LittleEndian(output.AsSpan(0x04)));
      Assert.Equal(FixtureGuid, new Guid(output.AsSpan(0x08, 16)));
      Assert.Equal(fixture, output);
    }
    finally
    {
      File.Delete(outputPath);
    }
  }

  [Fact]
  public void PublicReaderAndWriterConvertDynamicPositionsBetweenSourceAndGameCoordinates()
  {
    var fixture = CreateDefaultFixture();
    var serializedPosition = new System.Numerics.Vector3(1.25f, -2.5f, 3.75f);
    var serializedPosition2 = new System.Numerics.Vector3(-4.5f, 5.25f, -6.125f);
    WriteVector(fixture, ArchiveHeaderSize + 0x3EC, serializedPosition);
    WriteVector(fixture, ArchiveHeaderSize + 0x3F8, serializedPosition2);
    var outputPath = GetTemporaryPath();

    try
    {
      var mesh = ReadFixture(fixture);

      Assert.Equal(new System.Numerics.Vector3(1.25f, 2.5f, 3.75f), mesh.RootDynamic.Position1);
      Assert.Equal(new System.Numerics.Vector3(-4.5f, -5.25f, -6.125f), mesh.RootDynamic.Position2);

      new EarthMeshWriter(Encoding.UTF8).Write(mesh, outputPath);

      Assert.Equal(fixture, File.ReadAllBytes(outputPath));
    }
    finally
    {
      File.Delete(outputPath);
    }
  }

  [Fact]
  public void PublicReaderRejectsTrailingDataAfterDynamicRoot()
  {
    var path = GetTemporaryPath();
    try
    {
      File.WriteAllBytes(path, CreateDefaultFixture().Append((byte)0xFF).ToArray());

      var exception = Assert.Throws<InvalidDataException>(() => CreateReader().Read(path));

      Assert.Contains("trailing data", exception.Message, StringComparison.OrdinalIgnoreCase);
    }
    finally
    {
      File.Delete(path);
    }
  }

  [Theory]
  [InlineData(1)]
  [InlineData(16)]
  public void PublicReaderAndWriterRoundTripGroupingRootChildrenByteExactly(int childCount)
  {
    var children = Enumerable.Range(0, childCount)
      .Select(index => CreateDynamicRecord(
        (EffectType)((index % 14) + 1),
        new Vector3(index + 0.25f, index + 0.5f, index + 0.75f),
        new Vector3(-index - 1.25f, -index - 1.5f, -index - 1.75f)))
      .ToArray();
    var fixture = CreateFixture(CreateDynamicRecord(EffectType.Unknown, children: children));

    var mesh = ReadFixture(fixture);
    var parsedChildren = mesh.RootDynamic.SubMeshes.ToArray();

    Assert.Equal(EffectType.Unknown, mesh.RootDynamic.EffectType);
    Assert.Equal(string.Empty, mesh.RootDynamic.Model.FileName);
    Assert.Equal(string.Empty, mesh.RootDynamic.Texture.FileName);
    Assert.Equal(childCount, parsedChildren.Length);
    for (var index = 0; index < childCount; index++)
    {
      Assert.Equal((EffectType)((index % 14) + 1), parsedChildren[index].RootDynamic.EffectType);
      Assert.Equal(new Vector3(index + 0.25f, -index - 0.5f, index + 0.75f),
        parsedChildren[index].RootDynamic.Position1);
      Assert.Equal(new Vector3(-index - 1.25f, index + 1.5f, -index - 1.75f),
        parsedChildren[index].RootDynamic.Position2);
    }

    Assert.Equal(fixture, mesh.ToByteArray(Encoding.UTF8));
  }

  [Fact]
  public void PublicReaderAndWriterPreserveRecursiveChildRecordsWithoutArchivePreambles()
  {
    var grandchild = CreateDynamicRecord(EffectType.Smoke, new Vector3(1f, 2f, 3f));
    var child = CreateDynamicRecord(EffectType.Explosion, children: new[] { grandchild });
    var fixture = CreateFixture(CreateDynamicRecord(EffectType.Unknown, children: new[] { child }));

    var mesh = ReadFixture(fixture);
    var parsedChild = Assert.Single(mesh.RootDynamic.SubMeshes);
    var parsedGrandchild = Assert.Single(parsedChild.RootDynamic.SubMeshes);

    Assert.Equal(EffectType.Smoke, parsedGrandchild.RootDynamic.EffectType);
    Assert.Equal(new Vector3(1f, -2f, 3f), parsedGrandchild.RootDynamic.Position1);
    Assert.Equal("MESH"u8.ToArray(), fixture.AsSpan(ArchiveHeaderSize + DynamicRecordSize, 4).ToArray());
    Assert.Equal("MESH"u8.ToArray(), fixture.AsSpan(ArchiveHeaderSize + (2 * DynamicRecordSize), 4).ToArray());
    Assert.Equal(fixture, mesh.ToByteArray(Encoding.UTF8));
  }

  [Fact]
  public void PublicWriterUsesTheExactMaterializedDynamicChildCount()
  {
    var mesh = ReadFixture(CreateDefaultFixture());
    var child = ReadFixture(CreateFixture(CreateDynamicRecord(EffectType.Explosion)));
    mesh.RootDynamic = new DynamicPart
    {
      SubMeshes = new SingleUseEnumerable<IMesh>(new[] { child })
    };

    var output = mesh.ToByteArray(Encoding.UTF8);
    var roundTripped = ReadFixture(output);

    Assert.Single(roundTripped.RootDynamic.SubMeshes);
    Assert.Equal(ArchiveHeaderSize + (2 * DynamicRecordSize), output.Length);
  }

  [Theory]
  [MemberData(nameof(GetMalformedNestedFixtures))]
  public void PublicReaderRejectsMalformedNestedDynamicRecords(byte[] fixture, string expectedDetail)
  {
    var exception = Assert.Throws<InvalidDataException>(() => ReadFixture(fixture));

    Assert.Contains("DynamicObject.Children[0]", exception.Message, StringComparison.Ordinal);
    Assert.Contains(expectedDetail, exception.Message, StringComparison.OrdinalIgnoreCase);
  }

  [Fact]
  public void PublicReaderRejectsAnExtraDynamicChildBeyondTheDeclaredCount()
  {
    var root = CreateDynamicRecord(children: new[]
    {
      CreateDynamicRecord(EffectType.Explosion),
      CreateDynamicRecord(EffectType.Smoke)
    });
    WriteUInt32(root, 0x40C, 1);

    var exception = Assert.Throws<InvalidDataException>(() => ReadFixture(CreateFixture(root)));

    Assert.Contains("trailing data", exception.Message, StringComparison.OrdinalIgnoreCase);
  }

  [Fact]
  public void PublicDynamicEnumsRetainDocumentedNumericMeanings()
  {
    Assert.Equal(0, (int)EffectType.Unknown);
    Assert.Equal(1, (int)EffectType.Explosion);
    Assert.Equal(2, (int)EffectType.Track);
    Assert.Equal(3, (int)EffectType.ScaleableObject);
    Assert.Equal(4, (int)EffectType.MappedExplosion);
    Assert.Equal(5, (int)EffectType.FlatExplosion);
    Assert.Equal(6, (int)EffectType.Laser);
    Assert.Equal(7, (int)EffectType.LaserWall);
    Assert.Equal(8, (int)EffectType.Shockwave);
    Assert.Equal(9, (int)EffectType.Line);
    Assert.Equal(10, (int)EffectType.Sphere);
    Assert.Equal(11, (int)EffectType.ElectricalCannon);
    Assert.Equal(12, (int)EffectType.Lighting);
    Assert.Equal(13, (int)EffectType.Smoke);
    Assert.Equal(14, (int)EffectType.Keelwater);

    Assert.Equal(0, (int)LightType.Const);
    Assert.Equal(1, (int)LightType.Pyramid);
    Assert.Equal(2, (int)LightType.Trapezium);
    Assert.Equal(3, (int)LightType.Random);

    foreach (var effectType in Enum.GetValues<EffectType>())
    {
      var fixture = CreateDefaultFixture();
      WriteUInt32(fixture, ArchiveHeaderSize + 0x368, (uint)effectType);

      var mesh = ReadFixture(fixture);

      Assert.Equal(effectType, mesh.RootDynamic.EffectType);
      Assert.Equal((int)effectType,
        BinaryPrimitives.ReadInt32LittleEndian(mesh.ToByteArray(Encoding.UTF8).AsSpan(ArchiveHeaderSize + 0x368)));
    }

    foreach (var lightType in Enum.GetValues<LightType>())
    {
      var fixture = CreateDefaultFixture();
      WriteUInt32(fixture, ArchiveHeaderSize + 0x36C, (uint)lightType);

      var mesh = ReadFixture(fixture);

      Assert.Equal(lightType, mesh.RootDynamic.LightType);
      Assert.Equal((int)lightType,
        BinaryPrimitives.ReadInt32LittleEndian(mesh.ToByteArray(Encoding.UTF8).AsSpan(ArchiveHeaderSize + 0x36C)));
    }
  }

  private static byte[] CreateDefaultFixture()
  {
    return CreateFixture(CreateDynamicRecord());
  }

  private static byte[] CreateFixture(byte[] rootRecord)
  {
    var data = new byte[ArchiveHeaderSize + rootRecord.Length];
    WriteUInt32(data, 0x00, 0x30D0A1FF);
    WriteUInt32(data, 0x04, 1);
    FixtureGuid.ToByteArray().CopyTo(data, 0x08);
    rootRecord.CopyTo(data, ArchiveHeaderSize);
    return data;
  }

  private static byte[] CreateDynamicRecord(
    EffectType effectType = EffectType.Unknown,
    Vector3 position1 = default,
    Vector3 position2 = default,
    IReadOnlyList<byte[]>? children = null)
  {
    children ??= Array.Empty<byte[]>();
    var data = new byte[DynamicRecordSize + children.Sum(child => child.Length)];
    var meshOffset = 0;
    "MESH"u8.CopyTo(data.AsSpan(meshOffset));
    WriteUInt32(data, meshOffset + 0x04, MeshBaseHeader.SupportedVersion);
    WriteUInt32(data, meshOffset + 0x08, (uint)MeshKind.Dynamic);

    for (var slot = 0; slot < 49; slot++)
    {
      var slotOffset = meshOffset + 0x1D8 + (slot * 0x08);
      WriteInt16(data, slotOffset, short.MinValue);
      WriteInt16(data, slotOffset + 0x02, short.MinValue);
      WriteInt16(data, slotOffset + 0x04, short.MinValue);
    }

    WriteDefaultSize(data, meshOffset + 0x38C);
    WriteDefaultSize(data, meshOffset + 0x39C);
    WriteSingle(data, meshOffset + 0x3AC, 0.25f);
    WriteSingle(data, meshOffset + 0x3B0, 0.25f);
    WriteSingle(data, meshOffset + 0x3C8, 1f);
    WriteSingle(data, meshOffset + 0x3CC, 1f);
    WriteSingle(data, meshOffset + 0x3D0, 1f);
    WriteSingle(data, meshOffset + 0x3D4, 1f);
    WriteSingle(data, meshOffset + 0x3DC, 1f);
    WriteSingle(data, meshOffset + 0x3E0, 1f);
    WriteVector(data, meshOffset + 0x3EC, position1);
    WriteVector(data, meshOffset + 0x3F8, position2);
    WriteUInt32(data, meshOffset + 0x368, (uint)effectType);
    WriteUInt32(data, meshOffset + 0x40C, (uint)children.Count);
    var childOffset = DynamicRecordSize;
    foreach (var child in children)
    {
      child.CopyTo(data, childOffset);
      childOffset += child.Length;
    }

    return data;
  }

  public static TheoryData<byte[], string> GetMalformedNestedFixtures()
  {
    var staticChild = CreateDynamicRecord(EffectType.Explosion);
    WriteUInt32(staticChild, 0x08, (uint)MeshKind.Static);

    var truncatedChild = CreateDynamicRecord(EffectType.Explosion)[..^1];

    var nestedPreamble = CreateFixture(CreateDynamicRecord(EffectType.Explosion));

    return new TheoryData<byte[], string>
    {
      { CreateFixture(CreateDynamicRecord(children: new[] { staticChild })), "requires Dynamic" },
      { CreateFixture(CreateDynamicRecord(children: new[] { truncatedChild })), "requires" },
      { CreateFixture(CreateDynamicRecord(children: new[] { nestedPreamble })), "expected MESH" }
    };
  }

  private static void WriteDefaultSize(byte[] data, int offset)
  {
    WriteSingle(data, offset, -0.25f);
    WriteSingle(data, offset + 0x04, 0.25f);
    WriteSingle(data, offset + 0x08, 0.25f);
    WriteSingle(data, offset + 0x0C, -0.25f);
  }

  private static EarthMesh ReadFixture(byte[] fixture)
  {
    var path = GetTemporaryPath();
    try
    {
      File.WriteAllBytes(path, fixture);
      return Assert.IsType<EarthMesh>(CreateReader().Read(path));
    }
    finally
    {
      File.Delete(path);
    }
  }

  private static EarthMeshReader CreateReader()
  {
    return new EarthMeshReader(new EarthInfoFactory(Encoding.UTF8), new HierarchyBuilder(), Encoding.UTF8);
  }

  private static string GetTemporaryPath()
  {
    return Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid():N}.msh");
  }

  private static void WriteInt16(byte[] data, int offset, short value)
  {
    BinaryPrimitives.WriteInt16LittleEndian(data.AsSpan(offset), value);
  }

  private static void WriteUInt32(byte[] data, int offset, uint value)
  {
    BinaryPrimitives.WriteUInt32LittleEndian(data.AsSpan(offset), value);
  }

  private static void WriteSingle(byte[] data, int offset, float value)
  {
    WriteUInt32(data, offset, BitConverter.SingleToUInt32Bits(value));
  }

  private static void WriteVector(byte[] data, int offset, System.Numerics.Vector3 value)
  {
    WriteSingle(data, offset, value.X);
    WriteSingle(data, offset + sizeof(float), value.Y);
    WriteSingle(data, offset + (2 * sizeof(float)), value.Z);
  }

  private sealed class SingleUseEnumerable<T> : IEnumerable<T>
  {
    private readonly IEnumerable<T> _items;
    private bool _enumerated;

    public SingleUseEnumerable(IEnumerable<T> items)
    {
      _items = items;
    }

    public IEnumerator<T> GetEnumerator()
    {
      if (_enumerated)
      {
        return Enumerable.Empty<T>().GetEnumerator();
      }

      _enumerated = true;
      return _items.GetEnumerator();
    }

    IEnumerator IEnumerable.GetEnumerator()
    {
      return GetEnumerator();
    }
  }
}
