using EarthTool.Common.Factories;
using EarthTool.MSH.Interfaces;
using EarthTool.MSH.Models;
using EarthTool.MSH.Services;
using System.Buffers.Binary;
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
    var data = new byte[ArchiveHeaderSize + DynamicRecordSize];
    WriteUInt32(data, 0x00, 0x30D0A1FF);
    WriteUInt32(data, 0x04, 1);
    FixtureGuid.ToByteArray().CopyTo(data, 0x08);

    var meshOffset = ArchiveHeaderSize;
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
    return data;
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
}
