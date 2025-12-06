using EarthTool.PAR.Enums;
using EarthTool.PAR.GUI.Models;
using EarthTool.PAR.Models;
using ReactiveUI;
using System.Collections.ObjectModel;

namespace EarthTool.PAR.GUI.ViewModels;

/// <summary>
/// ViewModel for an entity group.
/// </summary>
public class EntityGroupViewModel : ViewModelBase
{
  private readonly EntityGroup _group;
  private bool _isExpanded;

  public EntityGroupViewModel(EntityGroup group)
  {
    _group = group ?? throw new System.ArgumentNullException(nameof(group));
    Entities = new ObservableCollection<EntityListItemViewModel>();

    // Load entities
    foreach (var entity in _group.Entities)
    {
      var editableEntity = new EditableEntity(entity);
      var entityVm = new EntityListItemViewModel(editableEntity);
      Entities.Add(entityVm);
    }
  }

  /// <summary>
  /// Gets the underlying entity group model.
  /// </summary>
  public EntityGroup Group => _group;

  /// <summary>
  /// Gets the group name (derived from entities).
  /// </summary>
  public string GroupName => _group.Name;

  /// <summary>
  /// Gets the faction.
  /// </summary>
  public Faction Faction => _group.Faction;

  /// <summary>
  /// Gets the group type.
  /// </summary>
  public EntityGroupType GroupType => _group.GroupType;

  /// <summary>
  /// Gets the entity count.
  /// </summary>
  public int EntityCount => Entities.Count;

  /// <summary>
  /// Gets the entities in this group.
  /// </summary>
  public ObservableCollection<EntityListItemViewModel> Entities { get; }

  /// <summary>
  /// Gets or sets whether the group is expanded in the UI.
  /// </summary>
  public bool IsExpanded
  {
    get => _isExpanded;
    set => this.RaiseAndSetIfChanged(ref _isExpanded, value);
  }

  /// <summary>
  /// Gets a display string for the group type.
  /// </summary>
  public string GroupTypeDisplay => GroupType.ToString();

  /// <summary>
  /// Gets a display string for the faction.
  /// </summary>
  public string FactionDisplay => Faction.ToString();
}
