using EarthTool.PAR.Enums;
using EarthTool.PAR.GUI.Models;
using ReactiveUI;

namespace EarthTool.PAR.GUI.ViewModels;

/// <summary>
/// ViewModel for an entity in the list.
/// </summary>
public class EntityListItemViewModel : ViewModelBase
{
  private readonly EditableEntity _editableEntity;
  private bool _isSelected;

  public EntityListItemViewModel(EditableEntity editableEntity)
  {
    _editableEntity = editableEntity ?? throw new System.ArgumentNullException(nameof(editableEntity));
  }

  /// <summary>
  /// Gets the editable entity.
  /// </summary>
  public EditableEntity EditableEntity => _editableEntity;

  /// <summary>
  /// Gets the entity name.
  /// </summary>
  public string Name => _editableEntity.DisplayName;

  /// <summary>
  /// Gets the type display name.
  /// </summary>
  public string TypeDisplayName => _editableEntity.TypeName;

  /// <summary>
  /// Gets the class type.
  /// </summary>
  public EntityClassType ClassType => _editableEntity.ClassType;

  /// <summary>
  /// Gets whether the entity has unsaved changes.
  /// </summary>
  public bool IsDirty => _editableEntity.IsDirty;

  /// <summary>
  /// Gets or sets whether the item is selected.
  /// </summary>
  public bool IsSelected
  {
    get => _isSelected;
    set => this.RaiseAndSetIfChanged(ref _isSelected, value);
  }

  /// <summary>
  /// Gets an icon for the entity type.
  /// </summary>
  public string Icon => GetIconForType(ClassType);

  /// <summary>
  /// Gets a tooltip with entity information.
  /// </summary>
  public string ToolTip => $"{TypeDisplayName}\nClass: {ClassType}{(IsDirty ? "\n(Modified)" : "")}";

  private static string GetIconForType(EntityClassType classType)
  {
    // Simple icon mapping - can be enhanced later
    return classType switch
    {
      EntityClassType.Building => "🏭",
      EntityClassType.Vehicle => "🚗",
      EntityClassType.Platoon => "👥",
      EntityClassType.Cannon => "💣",
      EntityClassType.Equipment => "⚙️",
      EntityClassType.Missile => "🚀",
      _ => "📄"
    };
  }
}
