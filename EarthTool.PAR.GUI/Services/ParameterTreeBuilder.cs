using EarthTool.PAR.GUI.Models;
using EarthTool.PAR.GUI.ViewModels;
using EarthTool.PAR.GUI.ViewModels.Details;
using EarthTool.PAR.Models;
using System.Collections.Generic;
using System.Linq;

namespace EarthTool.PAR.GUI.Services;

public class ParameterTreeBuilder
{
  private IEnumerable<EntityGroup> EntityGroups { get; set; }
  private IEnumerable<ResearchViewModel> Research { get; set; }

  public ParameterTreeBuilder()
  {
    EntityGroups = Enumerable.Empty<EntityGroup>();
    Research = Enumerable.Empty<ResearchViewModel>();
  }

  public ParameterTreeBuilder WithEntityGroups(IEnumerable<EntityGroup> entities)
  {
    EntityGroups = entities;
    return this;
  }

  public ParameterTreeBuilder WithResearch(IEnumerable<ResearchViewModel> research)
  {
    Research = research;
    return this;
  }

  public IEnumerable<ParameterTreeNode> Build(bool researchDependencyHierarchy = false)
  {
    var nodes = new List<ParameterTreeNode>();
    nodes.Add(new ParameterTreeNode("Entities", true, children: BuildEntityTree()));
    nodes.Add(new ParameterTreeNode("Research", true, children: BuildResearchTree(researchDependencyHierarchy)));
    return nodes;
  }

  private IEnumerable<ParameterTreeNode> BuildEntityTree()
    => EntityGroups.GroupBy(r => r.Faction)
      .Select(f => new ParameterTreeNode(f.Key.ToString(),
        children: f.GroupBy(g => g.GroupType)
          .Select(g => new ParameterTreeNode(g.Key.ToString(),
            children: g.SelectMany(eg => eg.Entities.Select(e => new ParameterTreeNode(e.Name, e)))))));

  private IEnumerable<ParameterTreeNode> BuildResearchTree(bool dependencyHierarchy = false)
  {
    if (dependencyHierarchy)
    {
      return Research.GroupBy(r => r.Faction)
        .Select(f => new ParameterTreeNode(f.Key.ToString(),
          children: f.GroupBy(g => g.Type)
            .Select(g => new ParameterTreeNode(g.Key.ToString(),
              children: g.Where(r => !r.RequiredResearch.Any()).Select(r => BuildResearchHierarchy(r, Research))))));
    }

    return Research.GroupBy(r => r.Faction)
      .Select(f => new ParameterTreeNode(f.Key.ToString(),
        children: f.GroupBy(g => g.Type)
          .Select(g => new ParameterTreeNode(g.Key.ToString(),
            children: g.Select(r => new ParameterTreeNode(r.Name, r))))));
  }

  private static ParameterTreeNode BuildResearchHierarchy(ResearchViewModel research, IEnumerable<ResearchViewModel> allResearch)
  {
    var children = allResearch
      .Where(r => r.RequiredResearch.Contains(research.Id))
      .Select(r => BuildResearchHierarchy(r, allResearch));
    return new ParameterTreeNode(research.Name, research, children: children);
  }
}