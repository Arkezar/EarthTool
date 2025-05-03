using ReactiveUI;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;

namespace EarthTool.PAR.GUI.Models;

public class ParameterTreeNode : ReactiveObject
{
  private ObservableCollection<ParameterTreeNode> _children;

  public string Name { get; }

  public object Entity { get; }

  public bool HasChildren => _children.Any();

  public IReadOnlyCollection<ParameterTreeNode> Children => _children;

  public ParameterTreeNode(
    string name,
    object entity = null,
    IEnumerable<ParameterTreeNode>? children = null)
  {
    Name = name;
    Entity = entity;

    _children = new ObservableCollection<ParameterTreeNode>(children ?? []);
  }
}