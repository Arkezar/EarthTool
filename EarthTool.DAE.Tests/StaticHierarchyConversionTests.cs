using Collada141;
using EarthTool.DAE.Elements;
using EarthTool.MSH.Enums;
using EarthTool.MSH.Interfaces;
using EarthTool.MSH.Models;
using EarthTool.MSH.Models.Collections;
using EarthTool.MSH.Models.Elements;
using EarthTool.MSH.Services;

namespace EarthTool.DAE.Tests;

public class StaticHierarchyConversionTests
{
  [Fact]
  public void MultiMaterialSourceObjectKeepsGeometryAndMaterialsUnderOneNode()
  {
    var collada = Convert(Part(PartType.Base), Part(PartType.ViewerFaced));

    Assert.Equal(2, Assert.Single(collada.Library_Geometries).Geometry.Count);
    Assert.Equal(2, Assert.Single(collada.Library_Materials).Material.Count);
    var root = GeometryRoot(collada);
    Assert.Single(root.Instance_Geometry);
    var partition = Assert.Single(root.NodeProperty);
    Assert.StartsWith("Part-0-1-", partition.Id, StringComparison.Ordinal);
  }

  [Fact]
  public void SiblingUnwindProducesSiblingGeometryNodes()
  {
    var collada = Convert(
      Part(PartType.Base),
      Part(PartType.Subpart),
      Part(PartType.Subpart, 1));

    var children = GeometryRoot(collada).NodeProperty;
    Assert.Equal(2, children.Count);
    Assert.All(children, child => Assert.Single(child.Instance_Geometry));
    Assert.StartsWith("Part-1-0-", children[0].Id, StringComparison.Ordinal);
    Assert.StartsWith("Part-2-0-", children[1].Id, StringComparison.Ordinal);
  }

  [Fact]
  public void AncestorPartitionReturnsGeometryToAncestorNode()
  {
    var collada = Convert(
      Part(PartType.Base),
      Part(PartType.Subpart),
      Part(PartType.Rotor, 1));

    var children = GeometryRoot(collada).NodeProperty;
    Assert.Equal(2, children.Count);
    Assert.StartsWith("Part-0-1-", children[0].Id, StringComparison.Ordinal);
    Assert.StartsWith("Part-1-0-", children[1].Id, StringComparison.Ordinal);
  }

  private static COLLADA Convert(params ModelPart[] parts)
  {
    var model = new EarthMesh
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
    var factory = new ColladaModelFactory(
      new AnimationsFactory(),
      new GeometriesFactory(),
      new MaterialFactory(),
      new LightingFactory(),
      new SlotFactory());

    return factory.GetColladaModel(model, "fixture");
  }

  private static Node GeometryRoot(COLLADA collada)
  {
    var scene = Assert.Single(collada.Library_Visual_Scenes).Visual_Scene.Single();
    var master = Assert.Single(scene.Node);
    return Assert.Single(master.NodeProperty);
  }

  private static ModelPart Part(PartType partType, byte unwind = 0)
  {
    return new ModelPart
    {
      PartType = partType,
      BackTrackDepth = unwind,
      Vertices = Array.Empty<IVertex>(),
      Faces = Array.Empty<IFace>(),
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
}
