using EarthTool.MSH.Interfaces;
using EarthTool.MSH.Models;
using System.Collections.Generic;
using System.Linq;

namespace EarthTool.MSH.Services
{
  public class HierarchyBuilder : IHierarchyBuilder
  {
    public PartNode GetPartsTree(IEnumerable<IModelPart> parts)
    {
      var currentId = 0;
      var root = new PartNode(currentId, parts.First());
      var lastNode = root;
      foreach (var part in parts.Skip(1))
      {
        var skip = part.BackTrackDepth;
        var parent = lastNode;
        for (var i = 0; i < skip; i++)
        {
          parent = parent.Parent;
        }
        lastNode = new PartNode(++currentId, part, parent);
        if (part.PartType == 0)
        {
          lastNode = parent;
        }
      }
      return root;
    }
  }
}