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
using System.Xml.Linq;

namespace EarthTool.DAE.Tests;

public class AttachmentConversionTests
{
  [Fact]
  public void PublicDaeConversionPreservesSparseAttachmentNumbers()
  {
    var daePath = Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid():N}.dae");
    var expectedNames = new[]
    {
      "Turret-3",
      "BarrelMuzzle-2",
      "TurretMuzzel-4",
      "UnloadPoint-2",
      "HitSpot-3",
      "SmokeSpot-4",
      "Unknown-2",
      "Chimney-2",
      "SmokeTrace-2",
      "Exhaust-2",
      "KeelTrace-2",
      "InterfacePivot-1",
      "CenterPivot-1",
      "ProductionSpotStart-1",
      "ProductionSpotEnd-1",
      "LandingSpot-1"
    };
    var expectedActiveIds = new[] { 3, 6, 12, 22, 27, 32, 34, 38, 40, 42, 44, 45, 46, 47, 48, 49 };

    try
    {
      new ColladaMeshWriter(CreateColladaModelFactory()).Write(CreateMesh(), daePath);

      var document = XDocument.Load(daePath);
      var exportedNames = document.Descendants()
        .Where(element => element.Name.LocalName == "light")
        .Select(element => (string)element.Attribute("name")!)
        .ToArray();
      Assert.Equal(expectedNames.Order(), exportedNames.Order());

      var converted = new ColladaMeshReader(new EarthInfoFactory(Encoding.UTF8), new HierarchyBuilder()).Read(daePath);
      var attachments = GetAttachments(converted.BaseHeader.Slots).ToArray();
      Assert.Equal(Enumerable.Range(1, 49), attachments.Select(attachment => attachment.Id));
      Assert.Equal(expectedActiveIds, attachments
        .Where(attachment => attachment.Id is < 13 or > 20 && attachment.IsValid)
        .Select(attachment => attachment.Id));
      foreach (var id in expectedActiveIds)
      {
        Assert.Equal(new System.Numerics.Vector3(id, id + 0.25f, -id), attachments[id - 1].Position.Value);
      }
    }
    finally
    {
      File.Delete(daePath);
    }
  }

  private static EarthMesh CreateMesh()
  {
    var part = new ModelPart
    {
      PartType = PartType.Base,
      Vertices = new IVertex[]
      {
        new Vertex(new Vector(0, 0, 0), new Vector(0, 0, 1), new TextureCoordinate(0, 0), 0, 0),
        new Vertex(new Vector(1, 0, 0), new Vector(0, 0, 1), new TextureCoordinate(1, 0), 0, 1),
        new Vertex(new Vector(0, 1, 0), new Vector(0, 0, 1), new TextureCoordinate(0, 1), 0, 2)
      },
      Faces = new IFace[] { new Face { V1 = 0, V2 = 1, V3 = 2, Flags = 1 } },
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
    => new()
    {
      Turrets = CreateSlots(4, 1, 3),
      BarrelMuzzels = CreateSlots(4, 5, 2),
      TurretMuzzels = CreateSlots(4, 9, 4),
      Headlights = CreateSlots(4, 13),
      Omnilights = CreateSlots(4, 17),
      UnloadPoints = CreateSlots(4, 21, 2),
      HitSpots = CreateSlots(4, 25, 3),
      SmokeSpots = CreateSlots(4, 29, 4),
      Unknown = CreateSlots(4, 33, 2),
      Chimneys = CreateSlots(2, 37, 2),
      SmokeTraces = CreateSlots(2, 39, 2),
      Exhausts = CreateSlots(2, 41, 2),
      KeelTraces = CreateSlots(2, 43, 2),
      InterfacePivot = CreateSlots(1, 45, 1),
      CenterPivot = CreateSlots(1, 46, 1),
      ProductionSpotStart = CreateSlots(1, 47, 1),
      ProductionSpotEnd = CreateSlots(1, 48, 1),
      LandingSpot = CreateSlots(1, 49, 1)
    };

  private static ISlot[] CreateSlots(int count, int firstId, params int[] activeNumbers)
  {
    return Enumerable.Range(0, count).Select(i =>
    {
      var id = firstId + i;
      var slot = new Slot { Id = id };
      if (activeNumbers.Contains(i + 1))
      {
        slot.Position = new Vector(id, id + 0.25f, -id);
        slot.Heading = 64;
        slot.FinalParameter = 0;
      }

      return (ISlot)slot;
    }).ToArray();
  }

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

  private static TInterface[] CreateRecords<TInterface, TRecord>(int count)
    where TRecord : TInterface, new()
    => Enumerable.Range(0, count).Select(_ => (TInterface)new TRecord()).ToArray();

  private static ColladaModelFactory CreateColladaModelFactory()
    => new(
      new AnimationsFactory(),
      new GeometriesFactory(),
      new MaterialFactory(),
      new LightingFactory(),
      new SlotFactory());
}
