using EarthTool.MSH.Models;
using System.Collections.Generic;

namespace EarthTool.MSH.Interfaces
{
  public interface IHierarchyBuilder
  {
    PartNode GetPartsTree(IEnumerable<IModelPart> parts);
  }
}