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

public class BarrelAngleConversionTests
{
  [Theory]
  [InlineData((byte)14)]
  [InlineData((byte)64)]
  [InlineData((byte)255)]
  public void MshToDaeToMshPreservesBarrelMaximumAngle(byte angle)
  {
    var result = ConvertThroughMshAndDae(PartType.Subpart | PartType.Barrel, angle * 360d / 256d);

    Assert.True(result.PartType.HasFlag(PartType.Barrel));
    Assert.Equal(angle * 360d / 256d, result.RiseAngle);
  }

  [Fact]
  public void MshToDaeToMshKeepsNonBarrelAngleAndFlagClear()
  {
    var result = ConvertThroughMshAndDae(PartType.Base, 0);

    Assert.False(result.PartType.HasFlag(PartType.Barrel));
    Assert.Equal(0, result.RiseAngle);
  }

  private static IModelPart ConvertThroughMshAndDae(PartType partType, double angle)
  {
    var inputMshPath = GetTemporaryPath("msh");
    var daePath = GetTemporaryPath("dae");
    var outputMshPath = GetTemporaryPath("msh");
    var mshReader = new EarthMeshReader(new EarthInfoFactory(Encoding.UTF8), new HierarchyBuilder(), Encoding.UTF8);

    try
    {
      new EarthMeshWriter(Encoding.UTF8).Write(CreateMesh(partType, angle), inputMshPath);
      var input = mshReader.Read(inputMshPath);
      new ColladaMeshWriter(CreateColladaModelFactory()).Write(input, daePath);
      var converted = new ColladaMeshReader(new EarthInfoFactory(Encoding.UTF8), new HierarchyBuilder()).Read(daePath);
      new EarthMeshWriter(Encoding.UTF8).Write(converted, outputMshPath);

      return Assert.Single(mshReader.Read(outputMshPath).Geometries);
    }
    finally
    {
      File.Delete(inputMshPath);
      File.Delete(daePath);
      File.Delete(outputMshPath);
    }
  }

  private static EarthMesh CreateMesh(PartType partType, double angle)
  {
    var part = new ModelPart
    {
      PartType = partType,
      RiseAngle = angle,
      Vertices = new IVertex[]
      {
        new Vertex(new Vector(0, 0, 0), new Vector(0, 0, 1), new TextureCoordinate(0, 0), 0, 0),
        new Vertex(new Vector(1, 0, 0), new Vector(0, 0, 1), new TextureCoordinate(1, 0), 0, 1),
        new Vertex(new Vector(0, 1, 0), new Vector(0, 0, 1), new TextureCoordinate(0, 1), 0, 2)
      },
      Faces = new IFace[] { new Face { V1 = 0, V2 = 1, V3 = 2, UNKNOWN = 1 } },
      Texture = new TextureInfo { FileName = "Textures\\fixture.tex" },
      Animations = new Animations(),
      Offset = new Vector()
    };

    return new EarthMesh
    {
      BaseHeader = new MeshBaseHeader
      {
        MeshKind = MeshKind.Static,
        Frames = new MeshFrames(),
        MountPoints = CreateRecords<IVector, Vector>(4),
        SpotLights = CreateRecords<ISpotLight, SpotLight>(4),
        OmnidirectionalLights = CreateRecords<IOmniLight, OmniLight>(4),
        Footprint = new MeshFootprint(),
        Slots = CreateSlots(),
        HorizontalExtents = new MeshHorizontalExtents()
      },
      Geometries = new[] { part },
      PartsTree = new HierarchyBuilder().GetPartsTree(new[] { part })
    };
  }

  private static ModelSlots CreateSlots()
  {
    return new ModelSlots
    {
      Turrets = CreateSlots(4),
      BarrelMuzzels = CreateSlots(4),
      TurretMuzzels = CreateSlots(4),
      Headlights = CreateSlots(4),
      Omnilights = CreateSlots(4),
      UnloadPoints = CreateSlots(4),
      HitSpots = CreateSlots(4),
      SmokeSpots = CreateSlots(4),
      Unknown = CreateSlots(4),
      Chimneys = CreateSlots(2),
      SmokeTraces = CreateSlots(2),
      Exhausts = CreateSlots(2),
      KeelTraces = CreateSlots(2),
      InterfacePivot = CreateSlots(1),
      CenterPivot = CreateSlots(1),
      ProductionSpotStart = CreateSlots(1),
      ProductionSpotEnd = CreateSlots(1),
      LandingSpot = CreateSlots(1)
    };
  }

  private static ISlot[] CreateSlots(int count)
  {
    return CreateRecords<ISlot, Slot>(count);
  }

  private static TInterface[] CreateRecords<TInterface, TRecord>(int count)
    where TRecord : TInterface, new()
  {
    return Enumerable.Range(0, count).Select(_ => (TInterface)new TRecord()).ToArray();
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

  private static string GetTemporaryPath(string extension)
  {
    return Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid():N}.{extension}");
  }
}
