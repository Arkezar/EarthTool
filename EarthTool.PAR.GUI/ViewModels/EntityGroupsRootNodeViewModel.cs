using System.Collections.ObjectModel;
using System.Linq;

namespace EarthTool.PAR.GUI.ViewModels;

/// <summary>
/// Root tree node for the Entity Groups section.
/// Example: "📋 Entity Groups"
/// </summary>
public class EntityGroupsRootNodeViewModel : TreeNodeViewModelBase
{
  private readonly ObservableCollection<TreeNodeViewModelBase> _children;

  public EntityGroupsRootNodeViewModel()
  {
    _children = new ObservableCollection<TreeNodeViewModelBase>();
    IsExpanded = true; // Entity Groups root expanded by default
  }

  /// <summary>
  /// Collection of Faction nodes
  /// </summary>
  public ObservableCollection<FactionNodeViewModel> Factions { get; }
    = new ObservableCollection<FactionNodeViewModel>();

  public override string Icon => "📋";

  public override string DisplayName
  {
    get
    {
      int totalGroups = Factions.Sum(f => f.ChildCount);
      return totalGroups > 0 ? $"Entity Groups ({totalGroups})" : "Entity Groups";
    }
  }

  public override ObservableCollection<TreeNodeViewModelBase>? Children
  {
    get
    {
      // Sync with Factions
      _children.Clear();
      foreach (var faction in Factions)
        _children.Add(faction);
      return _children;
    }
  }

  public override int ChildCount => Factions.Sum(f => f.ChildCount);
}
