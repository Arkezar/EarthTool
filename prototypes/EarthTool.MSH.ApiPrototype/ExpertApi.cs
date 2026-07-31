using System.Collections.Immutable;

namespace EarthTool.MSH.Expert;

public sealed record ExpertArchiveFramingInput(
  uint FramingWordRaw,
  uint? ArchiveTypeRaw,
  ImmutableArray<byte> GuidBytes);

public sealed record ExpertStaticMeshInput(
  ExpertArchiveFramingInput ArchiveFraming,
  BaseHeader BaseHeader,
  ImmutableArray<StaticRenderObject> StaticRenderObjects,
  uint StoredTrailingHierarchyUnwindCount,
  ImmutableArray<byte> RootTrailingData);

public sealed record ExpertDynamicMeshInput(
  ExpertArchiveFramingInput ArchiveFraming,
  DynamicObject RootDynamicObject,
  ImmutableArray<byte> RootTrailingData);

public static class MshExpert
{
  public static MshBuildResult<StaticMeshAsset> CreateStatic(
    ExpertStaticMeshInput input,
    MshSafetyProfile? safetyProfile = null)
    => throw new NotImplementedException("Prototype only");

  public static MshBuildResult<DynamicMeshAsset> CreateDynamic(
    ExpertDynamicMeshInput input,
    MshSafetyProfile? safetyProfile = null)
    => throw new NotImplementedException("Prototype only");
}
