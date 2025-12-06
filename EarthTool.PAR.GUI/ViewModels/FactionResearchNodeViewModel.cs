using EarthTool.PAR.Enums;
using System.Collections.ObjectModel;
using System.Linq;

namespace EarthTool.PAR.GUI.ViewModels;

/// <summary>
/// Tree node representing a Faction in research hierarchy (top level under Research root).
/// Example: "📁 UCS (8 research)"
/// </summary>
public class FactionResearchNodeViewModel : TreeNodeViewModelBase
{
  private readonly Faction _faction;
  private readonly ObservableCollection<TreeNodeViewModelBase> _children;

  public FactionResearchNodeViewModel(Faction faction)
  {
    _faction = faction;
    _children = new ObservableCollection<TreeNodeViewModelBase>();
  }

  /// <summary>
  /// The faction this node represents
  /// </summary>
  public Faction Faction => _faction;

  /// <summary>
  /// Collection of ResearchType nodes
  /// </summary>
  public ObservableCollection<ResearchTypeNodeViewModel> ResearchTypes { get; }
    = new ObservableCollection<ResearchTypeNodeViewModel>();

  public override string Icon => "📁";

  public override string DisplayName
  {
    get
    {
      int totalResearch = ResearchTypes
        .Where(rt => rt.IsVisible)
        .Sum(rt => rt.VisibleChildCount);
      return $"{_faction} ({totalResearch} research)";
    }
  }

  public override ObservableCollection<TreeNodeViewModelBase>? Children
  {
    get
    {
      // Sync with ResearchTypes
      _children.Clear();
      foreach (var rt in ResearchTypes)
        _children.Add(rt);
      return _children;
    }
  }

  public override int ChildCount => ResearchTypes.Sum(rt => rt.ChildCount);

  public override int VisibleChildCount => ResearchTypes
    .Where(rt => rt.IsVisible)
    .Sum(rt => rt.VisibleChildCount);
}
