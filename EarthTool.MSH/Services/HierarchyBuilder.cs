using EarthTool.MSH.Enums;
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
      var node = new PartNode(currentId++, parts.First());
      foreach (var part in parts.Skip(1))
      {
        var skip = part.BackTrackDepth;
        var parent = node;
        for (var i = 0; i < skip; i++)
        {
          parent = parent.Parent;
        }
    
        if (part.PartType == PartType.Base)
        {
          node = parent;
          node.Parts.Add(part);
        }
        else
        {
          node = new PartNode(currentId++, part, parent);
        }
      }

      while (node.Parent != null)
      {
        node = node.Parent;
      }
      return node;
    }
  }
}