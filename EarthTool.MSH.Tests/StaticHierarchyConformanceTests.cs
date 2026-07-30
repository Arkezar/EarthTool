using EarthTool.MSH.Enums;
using EarthTool.MSH.Interfaces;
using EarthTool.MSH.Models;
using EarthTool.MSH.Services;

namespace EarthTool.MSH.Tests;

public class StaticHierarchyConformanceTests
{
  [Fact]
  public void OnlyNestedSourceObjectFlagOpensHierarchyLevel()
  {
    var parts = new[]
    {
      Part(PartType.Base),
      Part(PartType.ViewerFaced),
      Part(PartType.Barrel),
      Part(PartType.Rotor),
      Part(PartType.Emitter1 | PartType.Emitter2 | PartType.Emitter3 | PartType.Emitter4)
    };

    var root = new HierarchyBuilder().GetPartsTree(parts);

    Assert.Equal(5, root.Parts.Count);
    Assert.Empty(root.Children);
  }

  [Fact]
  public void NestedFlagAndLowByteUnwindReconstructSiblingSourceObjects()
  {
    var parts = new[]
    {
      Part(PartType.Base),
      Part(PartType.Subpart),
      Part(PartType.Subpart, 1)
    };

    var root = new HierarchyBuilder().GetPartsTree(parts);

    Assert.Single(root.Parts);
    Assert.Equal(2, root.Children.Count);
    Assert.All(root.Children, child => Assert.Same(root, child.Parent));
  }

  [Fact]
  public void UnwindWithoutNestedFlagAddsMaterialPartitionToAncestor()
  {
    var rootPart = Part(PartType.Base);
    var childPart = Part(PartType.Subpart);
    var ancestorPartition = Part(PartType.ViewerFaced, 1);

    var root = new HierarchyBuilder().GetPartsTree(new[] { rootPart, childPart, ancestorPartition });

    Assert.Equal(new IModelPart[] { rootPart, ancestorPartition }, root.Parts);
    Assert.Single(root.Children);
    Assert.Equal(new IModelPart[] { childPart }, root.Children[0].Parts);
  }

  private static ModelPart Part(PartType partType, byte unwind = 0)
  {
    return new ModelPart
    {
      PartType = partType,
      BackTrackDepth = unwind
    };
  }
}
