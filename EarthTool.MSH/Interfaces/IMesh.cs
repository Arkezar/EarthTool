using EarthTool.Common.Interfaces;
using EarthTool.MSH.Models;
using System.Collections.Generic;

namespace EarthTool.MSH.Interfaces
{
  public interface IMesh : IBinarySerializable
  {
    IMeshBaseHeader BaseHeader { get; }
    IEarthInfo FileHeader { get; }
    IEnumerable<IModelPart> Geometries { get; }
    PartNode PartsTree { get; }
    IDynamicPart RootDynamic { get; }
    uint? TrailingHierarchyUnwindCount { get; }
  }
}
