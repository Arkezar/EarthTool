using EarthTool.CLI.Commands.MSH;
using EarthTool.MSH.Interfaces;
using EarthTool.MSH.Models;

namespace EarthTool.DAE.Tests;

public class DynamicInspectionTests
{
  [Fact]
  public void DynamicInspectionTraversesTheCompleteTreeInChildFirstOrder()
  {
    var grandchild = CreateMesh(EffectType.Smoke);
    var child = CreateMesh(EffectType.Explosion, grandchild);
    var sibling = CreateMesh(EffectType.Line);
    var root = new DynamicPart
    {
      EffectType = EffectType.Unknown,
      SubMeshes = new[] { child, sibling }
    };

    var effectTypes = ConvertCommand.EnumerateDynamicParts(root)
      .Select(part => part.EffectType)
      .ToArray();

    Assert.Equal(
      new[] { EffectType.Smoke, EffectType.Explosion, EffectType.Line, EffectType.Unknown },
      effectTypes);
  }

  private static EarthMesh CreateMesh(EffectType effectType, params IMesh[] children)
  {
    return new EarthMesh
    {
      BaseHeader = new MeshBaseHeader { MeshKind = MeshKind.Dynamic },
      RootDynamic = new DynamicPart
      {
        EffectType = effectType,
        SubMeshes = children
      }
    };
  }
}
