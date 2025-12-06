using EarthTool.PAR.Enums;
using System.Collections.ObjectModel;

namespace EarthTool.PAR.GUI.ViewModels;

/// <summary>
/// Tree node representing an EntityGroupType (second level).
/// Example: "📂 Vehicle (5 groups)"
/// </summary>
public class GroupTypeNodeViewModel : TreeNodeViewModelBase
{
  private readonly EntityGroupType _groupType;
  private readonly ObservableCollection<TreeNodeViewModelBase> _children;

  public GroupTypeNodeViewModel(EntityGroupType groupType)
  {
    _groupType = groupType;
    _children = new ObservableCollection<TreeNodeViewModelBase>();
  }

  /// <summary>
  /// The group type this node represents
  /// </summary>
  public EntityGroupType GroupType => _groupType;

  /// <summary>
  /// Collection of EntityGroup nodes
  /// </summary>
  public ObservableCollection<EntityGroupNodeViewModel> EntityGroups { get; }
    = new ObservableCollection<EntityGroupNodeViewModel>();

  public override string Icon => "📂";

  public override string DisplayName => $"{_groupType} ({VisibleChildCount} groups)";

  public override ObservableCollection<TreeNodeViewModelBase>? Children
  {
    get
    {
      // Sync with EntityGroups
      _children.Clear();
      foreach (var eg in EntityGroups)
        _children.Add(eg);
      return _children;
    }
  }

  public override int ChildCount => EntityGroups.Count;
}
