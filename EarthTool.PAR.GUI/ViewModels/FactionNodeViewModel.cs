using EarthTool.PAR.Enums;
using System.Collections.ObjectModel;
using System.Linq;

namespace EarthTool.PAR.GUI.ViewModels;

/// <summary>
/// Tree node representing a Faction (top level).
/// Example: "📁 UCS (25 groups)"
/// </summary>
public class FactionNodeViewModel : TreeNodeViewModelBase
{
  private readonly Faction _faction;
  private readonly ObservableCollection<TreeNodeViewModelBase> _children;

  public FactionNodeViewModel(Faction faction)
  {
    _faction = faction;
    _children = new ObservableCollection<TreeNodeViewModelBase>();
  }

  /// <summary>
  /// The faction this node represents
  /// </summary>
  public Faction Faction => _faction;

  /// <summary>
  /// Collection of GroupType nodes
  /// </summary>
  public ObservableCollection<GroupTypeNodeViewModel> GroupTypes { get; }
    = new ObservableCollection<GroupTypeNodeViewModel>();

  public override string Icon => "📁";

  public override string DisplayName
  {
    get
    {
      int totalGroups = GroupTypes
        .Where(gt => gt.IsVisible)
        .Sum(gt => gt.VisibleChildCount);
      return $"{_faction} ({totalGroups} groups)";
    }
  }

  public override ObservableCollection<TreeNodeViewModelBase>? Children
  {
    get
    {
      // Sync with GroupTypes
      _children.Clear();
      foreach (var gt in GroupTypes)
        _children.Add(gt);
      return _children;
    }
  }

  public override int ChildCount => GroupTypes.Sum(gt => gt.ChildCount);

  public override int VisibleChildCount => GroupTypes
    .Where(gt => gt.IsVisible)
    .Sum(gt => gt.VisibleChildCount);
}
