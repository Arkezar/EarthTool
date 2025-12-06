using System.Collections.ObjectModel;
using System.Linq;

namespace EarthTool.PAR.GUI.ViewModels;

/// <summary>
/// Root tree node for the Research section.
/// Example: "📚 Research"
/// </summary>
public class ResearchRootNodeViewModel : TreeNodeViewModelBase
{
  private readonly ObservableCollection<TreeNodeViewModelBase> _children;

  public ResearchRootNodeViewModel()
  {
    _children = new ObservableCollection<TreeNodeViewModelBase>();
    IsExpanded = true; // Research root expanded by default
  }

  /// <summary>
  /// Collection of Faction nodes
  /// </summary>
  public ObservableCollection<FactionResearchNodeViewModel> Factions { get; }
    = new ObservableCollection<FactionResearchNodeViewModel>();

  public override string Icon => "📚";

  public override string DisplayName
  {
    get
    {
      int totalResearch = Factions.Sum(f => f.ChildCount);
      return totalResearch > 0 ? $"Research ({totalResearch})" : "Research";
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
