using EarthTool.PAR.GUI.Models;
using EarthTool.PAR.GUI.Services;
using Microsoft.Extensions.Logging;
using ReactiveUI;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Reactive;
using System.Reactive.Linq;

namespace EarthTool.PAR.GUI.ViewModels;

/// <summary>
/// ViewModel for displaying and editing entity details.
/// </summary>
public class EntityDetailsViewModel : ViewModelBase
{
  private readonly IPropertyEditorFactory _propertyEditorFactory;
  private readonly IEntityValidationService _validationService;
  private readonly ILogger<EntityDetailsViewModel> _logger;
  private EditableEntity? _currentEntity;
  private string _entityName = string.Empty;

  public EntityDetailsViewModel(
    IPropertyEditorFactory propertyEditorFactory,
    IEntityValidationService validationService,
    ILogger<EntityDetailsViewModel> logger)
  {
    _propertyEditorFactory = propertyEditorFactory ?? throw new ArgumentNullException(nameof(propertyEditorFactory));
    _validationService = validationService ?? throw new ArgumentNullException(nameof(validationService));
    _logger = logger ?? throw new ArgumentNullException(nameof(logger));

    PropertyGroups = new ObservableCollection<PropertyGroupViewModel>();
    ValidationErrors = new ObservableCollection<ValidationError>();

    InitializeCommands();
  }

  /// <summary>
  /// Gets or sets the current entity being edited.
  /// </summary>
  public EditableEntity? CurrentEntity
  {
    get => _currentEntity;
    set
    {
      if (_currentEntity == value) return;
      
      _currentEntity = value;
      this.RaisePropertyChanged();
      LoadEntity();
    }
  }

  /// <summary>
  /// Gets or sets the entity name.
  /// </summary>
  public string EntityName
  {
    get => _entityName;
    set
    {
      if (_entityName == value) return;
      
      _entityName = value;
      this.RaisePropertyChanged();
      
      if (_currentEntity != null)
      {
        _currentEntity.Entity.Name = value;
        _currentEntity.MarkDirty();
      }
    }
  }

  /// <summary>
  /// Gets the entity type display name.
  /// </summary>
  public string EntityType => _currentEntity?.TypeName ?? string.Empty;

  /// <summary>
  /// Gets the entity class type display.
  /// </summary>
  public string ClassType => _currentEntity?.ClassType.ToString() ?? string.Empty;

  /// <summary>
  /// Gets the collection of property groups.
  /// </summary>
  public ObservableCollection<PropertyGroupViewModel> PropertyGroups { get; }

  /// <summary>
  /// Gets the collection of validation errors.
  /// </summary>
  public ObservableCollection<ValidationError> ValidationErrors { get; }

  /// <summary>
  /// Gets whether there are validation errors.
  /// </summary>
  public bool HasErrors => ValidationErrors.Any(e => e.Severity == ValidationSeverity.Error);

  /// <summary>
  /// Gets whether there are any validation issues.
  /// </summary>
  public bool HasValidationIssues => ValidationErrors.Any();

  public ReactiveCommand<Unit, Unit> RevertChangesCommand { get; private set; } = null!;
  public ReactiveCommand<Unit, Unit> ApplyChangesCommand { get; private set; } = null!;
  public ReactiveCommand<Unit, Unit> ValidateCommand { get; private set; } = null!;

  private void InitializeCommands()
  {
    var hasEntity = this.WhenAnyValue(x => x.CurrentEntity).Select(entity => entity != null);
    var isDirty = this.WhenAnyValue(x => x.CurrentEntity).Select(e => e?.IsDirty ?? false);

    RevertChangesCommand = ReactiveCommand.Create(RevertChanges, isDirty);
    ApplyChangesCommand = ReactiveCommand.Create(ApplyChanges, isDirty);
    ValidateCommand = ReactiveCommand.Create(Validate, hasEntity);
  }

  private void LoadEntity()
  {
    PropertyGroups.Clear();
    ValidationErrors.Clear();

    if (_currentEntity == null)
    {
      _logger.LogDebug("Entity details cleared");
      return;
    }

    _entityName = _currentEntity.DisplayName;
    this.RaisePropertyChanged(nameof(EntityName));
    this.RaisePropertyChanged(nameof(EntityType));
    this.RaisePropertyChanged(nameof(ClassType));

    // Create property editors
    var editors = _propertyEditorFactory.CreateEditorsForEntity(_currentEntity.Entity);
    var groupedEditors = GroupProperties(editors);

    foreach (var group in groupedEditors)
    {
      PropertyGroups.Add(group);
    }

    // Validate
    Validate();

    _logger.LogInformation("Loaded entity details for '{Name}' ({Type}) with {PropertyCount} properties",
      _currentEntity.DisplayName, _currentEntity.TypeName, editors.Count());
  }

  private IEnumerable<PropertyGroupViewModel> GroupProperties(IEnumerable<PropertyEditorViewModel> editors)
  {
    var groups = new Dictionary<string, List<PropertyEditorViewModel>>();

    foreach (var editor in editors)
    {
      var groupName = DeterminePropertyGroup(editor.PropertyName);

      if (!groups.ContainsKey(groupName))
      {
        groups[groupName] = new List<PropertyEditorViewModel>();
      }

      groups[groupName].Add(editor);
    }

    // Create ViewModels for each group
    var groupViewModels = new List<PropertyGroupViewModel>();

    // Define group order
    var groupOrder = new[] { "Basic", "Combat", "Movement", "Equipment", "Building", "Special", "References", "Other" };

    foreach (var groupName in groupOrder)
    {
      if (groups.TryGetValue(groupName, out var groupEditors))
      {
        groupViewModels.Add(new PropertyGroupViewModel
        {
          GroupName = groupName,
          Properties = new ObservableCollection<PropertyEditorViewModel>(groupEditors)
        });
      }
    }

    return groupViewModels;
  }

  private string DeterminePropertyGroup(string propertyName)
  {
    // Group properties by logical categories
    if (propertyName is "Name" or "Cost" or "TimeOfBuild" or "Mesh")
      return "Basic";

    if (propertyName is "HP" or "Armor" or "HpRegeneration" or "CalorificCapacity" or "DisableResist")
      return "Combat";

    if (propertyName.Contains("Speed") || propertyName.Contains("Range"))
      return "Movement";

    if (propertyName.Contains("Slot") || propertyName.Contains("Equipment") || propertyName.Contains("Cannon"))
      return "Equipment";

    if (propertyName.Contains("Building") || propertyName.Contains("Power") || propertyName.Contains("Resource"))
      return "Building";

    if (propertyName.EndsWith("Id") || propertyName.Contains("Ref"))
      return "References";

    if (propertyName.Contains("Explosion") || propertyName.Contains("Smoke") || propertyName.Contains("Effect"))
      return "Special";

    return "Other";
  }

  private void RevertChanges()
  {
    if (_currentEntity == null)
      return;

    _currentEntity.RevertChanges();
    LoadEntity(); // Reload to refresh all editors
    _logger.LogInformation("Reverted changes for entity '{Name}'", _currentEntity.DisplayName);
  }

  private void ApplyChanges()
  {
    if (_currentEntity == null)
      return;

    _currentEntity.AcceptChanges();
    _logger.LogInformation("Applied changes for entity '{Name}'", _currentEntity.DisplayName);
  }

  private void Validate()
  {
    ValidationErrors.Clear();

    if (_currentEntity == null)
      return;

    var result = _validationService.Validate(_currentEntity.Entity);

    foreach (var error in result.Errors)
    {
      ValidationErrors.Add(error);
    }

    this.RaisePropertyChanged(nameof(HasErrors));
    this.RaisePropertyChanged(nameof(HasValidationIssues));

    _logger.LogDebug("Validation completed: {ErrorCount} errors, {WarningCount} warnings",
      result.Errors.Count(e => e.Severity == ValidationSeverity.Error),
      result.Errors.Count(e => e.Severity == ValidationSeverity.Warning));
  }
}

/// <summary>
/// ViewModel for a group of properties.
/// </summary>
public class PropertyGroupViewModel : ViewModelBase
{
  private string _groupName = string.Empty;
  private bool _isExpanded = true;

  /// <summary>
  /// Gets or sets the group name.
  /// </summary>
  public string GroupName
  {
    get => _groupName;
    set => this.RaiseAndSetIfChanged(ref _groupName, value);
  }

  /// <summary>
  /// Gets or sets whether the group is expanded.
  /// </summary>
  public bool IsExpanded
  {
    get => _isExpanded;
    set => this.RaiseAndSetIfChanged(ref _isExpanded, value);
  }

  /// <summary>
  /// Gets the collection of properties in this group.
  /// </summary>
  public ObservableCollection<PropertyEditorViewModel> Properties { get; set; } = new();
}
