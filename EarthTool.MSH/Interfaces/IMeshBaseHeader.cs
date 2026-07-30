using EarthTool.Common.Interfaces;
using System.Collections.Generic;

namespace EarthTool.MSH.Interfaces
{
  public interface IMeshBaseHeader : IBinarySerializable
  {
    /// <summary>Gets the complete 32-bit logical box presence mask.</summary>
    uint BoxPresenceMask { get; }
    IMeshFrames Frames { get; }
    IEnumerable<IVector> MountPoints { get; }
    IReadOnlyList<ISpotLight> SpotLights { get; }
    IReadOnlyList<IOmniLight> OmnidirectionalLights { get; }
    IModelSlots Slots { get; }
    /// <summary>Gets the logical box and raw coverage data.</summary>
    IMeshFootprint Footprint { get; }

    /// <summary>Gets unsigned horizontal magnitudes in +Y, -Y, +X, -X order.</summary>
    IMeshHorizontalExtents HorizontalExtents { get; }
    MeshKind MeshKind { get; }
  }
}
