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
    IEnumerable<IOmniLight> OmniLights { get; }
    IModelSlots Slots { get; }
    ITemplateDetails TemplateDetails { get; }
    IMeshBoundries Boundries { get; }
    int Type { get; }
    int UnknownValue1 { get; }
    int UnknownValue2 { get; }
  }
}