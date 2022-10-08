using EarthTool.Common.Interfaces;
using EarthTool.MSH.Models.Elements;
using System.Collections.Generic;

namespace EarthTool.MSH.Interfaces
{
  public interface IMeshDescriptor : IBinarySerializable
  {
    IModelTemplate Template { get; }
    IMeshFrames Frames { get; }
    IEnumerable<IVector> MountPoints { get; }
    IEnumerable<ISpotLight> SpotLights { get; }
    IEnumerable<IOmniLight> OmnidirectionalLights { get; }
    IModelSlots Slots { get; }
    ITemplateDetails TemplateDetails { get; }
    IMeshBoundries Boundaries { get; }
    MeshType MeshType { get; }
    MeshSubType? RegularMeshSubType { get; }
    DynamicMeshSubType? DynamicMeshSubType { get; }
  }
}