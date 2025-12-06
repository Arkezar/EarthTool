using EarthTool.PAR.Enums;
using EarthTool.PAR.GUI.Models;
using EarthTool.PAR.Models;
using System;
using System.Collections.ObjectModel;

namespace EarthTool.PAR.GUI.ViewModels;

/// <summary>
/// Tree node representing an EntityGroup (third level).
/// Example: "📋 Tank, Artillery (15 entities)"
/// Replaces the old EntityGroupViewModel.
/// </summary>
public class EntityGroupNodeViewModel : TreeNodeViewModelBase
{
  private readonly EntityGroup _group;
  private readonly ObservableCollection<TreeNodeViewModelBase> _children;

  public EntityGroupNodeViewModel(EntityGroup group)
  {
    _group = group ?? throw new ArgumentNullException(nameof(group));
    _children = new ObservableCollection<TreeNodeViewModelBase>();

    // Load entities as tree nodes
    foreach (var entity in _group.Entities)
    {
      var editableEntity = new EditableEntity(entity);
      var entityVm = new EntityListItemViewModel(editableEntity);
      Entities.Add(entityVm);
    }
  }

  /// <summary>
  /// The underlying entity group model
  /// </summary>
  public EntityGroup Group => _group;

  /// <summary>
  /// Collection of Entity nodes (leaf level)
  /// </summary>
  public ObservableCollection<EntityListItemViewModel> Entities { get; }
    = new ObservableCollection<EntityListItemViewModel>();

  public override string Icon => "📋";

  public override string DisplayName => $"{_group.Name} ({VisibleChildCount} entities)";

  public override ObservableCollection<TreeNodeViewModelBase>? Children
  {
    get
    {
      // Sync with Entities
      _children.Clear();
      foreach (var entity in Entities)
        _children.Add(entity);
      return _children;
    }
  }

  public override int ChildCount => Entities.Count;

  /// <summary>
  /// Faction (for reference, not displayed at this level)
  /// </summary>
  public Faction Faction => _group.Faction;

  /// <summary>
  /// GroupType (for reference, not displayed at this level)
  /// </summary>
  public EntityGroupType GroupType => _group.GroupType;
}
