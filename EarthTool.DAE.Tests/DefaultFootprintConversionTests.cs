using AwesomeAssertions;
using EarthTool.Common.Factories;
using EarthTool.DAE.Elements;
using EarthTool.DAE.Services;
using EarthTool.MSH.Enums;
using EarthTool.MSH.Interfaces;
using EarthTool.MSH.Models;
using EarthTool.MSH.Models.Collections;
using EarthTool.MSH.Models.Elements;
using EarthTool.MSH.Services;
using System.Text;

namespace EarthTool.DAE.Tests;

public class DefaultFootprintConversionTests
{
  [Fact]
  public void PublicDaeImportGeneratesDefaultFootprintFromRootGeometry()
  {
    var daePath = Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid():N}.dae");

    try
    {
      new ColladaMeshWriter(CreateColladaModelFactory()).Write(CreateMesh(), daePath);

      var converted = new ColladaMeshReader(new EarthInfoFactory(Encoding.UTF8), new HierarchyBuilder()).Read(daePath);

      converted.BaseHeader.BoxPresenceMask.Should().Be(0x00008000u);
      converted.BaseHeader.Footprint.BoxHeights[15].Should().Be((ushort)448);
      converted.BaseHeader.Footprint.BoxHeights.Take(15).Should().OnlyContain(height => height == 0);
      converted.BaseHeader.Footprint.BoxFlags.Should().OnlyContain(flag => flag == 0);
      converted.BaseHeader.Footprint.CoverageDescriptors.Should().Equal(
        0x3A000008,
        0x00008000,
        0xCA001000,
        0xFF000001);
      converted.BaseHeader.Footprint.CoverageBitmaps.Should().Equal(
        new ulong[]
        {
          0xFFFFFFFFFFFF0FFF,
          0x0FFFFFFFFFFFFFFF,
          0xFFF0FFFFFFFFFFFF,
          0xFFFFFFFFFFFFFFF0
        });
      converted.BaseHeader.HorizontalExtents.PositiveY.Should().Be((ushort)1088);
      converted.BaseHeader.HorizontalExtents.NegativeY.Should().Be((ushort)896);
      converted.BaseHeader.HorizontalExtents.PositiveX.Should().Be((ushort)640);
      converted.BaseHeader.HorizontalExtents.NegativeX.Should().Be((ushort)320);
    }
    finally
    {
      File.Delete(daePath);
    }
  }

  [Fact]
  public async Task DaeCliConversionWritesMeshAcceptedByPublicParser()
  {
    var daePath = Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid():N}.dae");
    var mshPath = Path.ChangeExtension(daePath, ".msh");

    try
    {
      new ColladaMeshWriter(CreateColladaModelFactory()).Write(CreateMesh(), daePath);

      await EarthTool.CLI.Program.Main(new[] { "dae", daePath });

      var converted = new EarthMeshReader(
        new EarthInfoFactory(Encoding.UTF8),
        new HierarchyBuilder(),
        Encoding.UTF8).Read(mshPath);
      converted.BaseHeader.BoxPresenceMask.Should().Be(0x00008000u);
      converted.BaseHeader.Footprint.BoxHeights[15].Should().Be((ushort)448);
    }
    finally
    {
      File.Delete(daePath);
      File.Delete(mshPath);
    }
  }

  private static EarthMesh CreateMesh()
  {
    var parts = new[]
    {
      CreatePart(PartType.Base,
        new Vector(-1.25f, -3.5f, 1.75f),
        new Vector(2.5f, 4.25f, 0),
        new Vector(0, 0, 1)),
      CreatePart(PartType.Subpart,
        new Vector(-20, -20, 9),
        new Vector(20, 20, 9),
        new Vector(0, 0, 9))
    };

    return new EarthMesh
    {
      BaseHeader = new MeshBaseHeader
      {
        MeshKind = MeshKind.Static,
        SpotLights = Array.Empty<ISpotLight>(),
        OmnidirectionalLights = Array.Empty<IOmniLight>(),
        Slots = EmptySlots()
      },
      Geometries = parts,
      PartsTree = new HierarchyBuilder().GetPartsTree(parts)
    };
  }

  private static ModelPart CreatePart(PartType partType, params Vector[] positions)
  {
    var vertices = positions.Select((position, index) => (IVertex)new Vertex(
      position,
      new Vector(0, 0, 1),
      new TextureCoordinate(index == 1 ? 1 : 0, index == 2 ? 1 : 0),
      checked((ushort)index),
      checked((ushort)index))).ToArray();

    return new ModelPart
    {
      PartType = partType,
      Vertices = vertices,
      Faces = new IFace[] { new Face { V1 = 0, V2 = 1, V3 = 2, Flags = 1 } },
      Texture = new TextureInfo { FileName = "Textures\\fixture.tex" },
      Animations = new Animations(),
      Offset = new Vector()
    };
  }

  private static ModelSlots EmptySlots()
  {
    var slots = Array.Empty<ISlot>();
    return new ModelSlots
    {
      Turrets = slots,
      BarrelMuzzels = slots,
      TurretMuzzels = slots,
      Headlights = slots,
      Omnilights = slots,
      UnloadPoints = slots,
      HitSpots = slots,
      SmokeSpots = slots,
      Unknown = slots,
      Chimneys = slots,
      SmokeTraces = slots,
      Exhausts = slots,
      KeelTraces = slots,
      InterfacePivot = slots,
      CenterPivot = slots,
      ProductionSpotStart = slots,
      ProductionSpotEnd = slots,
      LandingSpot = slots
    };
  }

  private static ColladaModelFactory CreateColladaModelFactory()
  {
    return new ColladaModelFactory(
      new AnimationsFactory(),
      new GeometriesFactory(),
      new MaterialFactory(),
      new LightingFactory(),
      new SlotFactory());
  }
}
