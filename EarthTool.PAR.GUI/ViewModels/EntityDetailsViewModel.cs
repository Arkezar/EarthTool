using EarthTool.PAR.GUI.Models;
using EarthTool.PAR.GUI.Services;
using EarthTool.PAR.Models;
using Microsoft.Extensions.Logging;
using ReactiveUI;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Reactive;
using System.Reactive.Linq;
using System.Reflection;

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
  private Research? _currentResearch;
  private string _entityName = string.Empty;
  private ParFile? _parFile;
  private Action<string>? _navigateToResearch;

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
      LoadEntityOrResearch();
    }
  }

  /// <summary>
  /// Gets or sets the current research being edited.
  /// </summary>
  public Research? CurrentResearch
  {
    get => _currentResearch;
    set
    {
      if (_currentResearch == value) return;
      
      _currentResearch = value;
      this.RaisePropertyChanged();
      LoadEntityOrResearch();
    }
  }

  /// <summary>
  /// Gets or sets the ParFile context for research lookups.
  /// </summary>
  public ParFile? ParFile
  {
    get => _parFile;
    set
    {
      if (_parFile == value) return;
      
      _parFile = value;
      this.RaisePropertyChanged();
      
      // Reload if we have an active entity/research
      if (_currentEntity != null || _currentResearch != null)
      {
        LoadEntityOrResearch();
      }
    }
  }

  /// <summary>
  /// Gets or sets the callback for navigating to research by name.
  /// </summary>
  public Action<string>? NavigateToResearch
  {
    get => _navigateToResearch;
    set
    {
      if (_navigateToResearch == value) return;
      
      _navigateToResearch = value;
      this.RaisePropertyChanged();
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
  public string EntityType => _currentEntity?.TypeName ?? _currentResearch?.Type.ToString() ?? string.Empty;

  /// <summary>
  /// Gets the entity class type display.
  /// </summary>
  public string ClassType => _currentEntity?.ClassType.ToString() ?? _currentResearch?.Faction.ToString() ?? string.Empty;

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
    
    // Create an observable that monitors IsDirty changes on the CurrentEntity
    var isDirty = this.WhenAnyValue(x => x.CurrentEntity)
      .Select(entity => entity != null 
        ? entity.WhenAnyValue(e => e.IsDirty).StartWith(entity.IsDirty)
        : Observable.Return(false))
      .Switch();

    RevertChangesCommand = ReactiveCommand.Create(RevertChanges, isDirty);
    ApplyChangesCommand = ReactiveCommand.Create(ApplyChanges, isDirty);
    ValidateCommand = ReactiveCommand.Create(Validate, hasEntity);
  }

  private void LoadEntityOrResearch()
  {
    if (_currentEntity != null)
    {
      // Istniejąca logika dla encji
      LoadEntity();
    }
    else if (_currentResearch != null)
    {
      // Nowa logika dla badań
      LoadResearch();
    }
    else
    {
      // Clear when no entity/research is selected
      PropertyGroups.Clear();
      ValidationErrors.Clear();
      
      _logger.LogDebug("Details cleared");
      _entityName = string.Empty;
      this.RaisePropertyChanged(nameof(EntityName));
      this.RaisePropertyChanged(nameof(EntityType));
      this.RaisePropertyChanged(nameof(ClassType));
    }
  }

  private IEnumerable<PropertyGroupViewModel> GroupResearchProperties(IEnumerable<PropertyEditorViewModel> editors)
  {
    if (_currentResearch == null)
      return Enumerable.Empty<PropertyGroupViewModel>();

    // Group properties by declaring type using reflection
    var researchType = _currentResearch.GetType();
    var propertiesByType = new Dictionary<Type, List<PropertyEditorViewModel>>();

    foreach (var editor in editors)
    {
      // Find which type in the hierarchy declares this property
      var declaringType = FindDeclaringType(researchType, editor.PropertyName);
      
      if (declaringType != null)
      {
        if (!propertiesByType.ContainsKey(declaringType))
        {
          propertiesByType[declaringType] = new List<PropertyEditorViewModel>();
        }
        propertiesByType[declaringType].Add(editor);
      }
    }

    // Build inheritance hierarchy from base to derived
    var hierarchy = new List<Type>();
    var currentType = researchType;
    while (currentType != null && currentType != typeof(object))
    {
      hierarchy.Add(currentType);
      currentType = currentType.BaseType;
    }
    hierarchy.Reverse(); // Now ordered from base to derived

    // Create groups in hierarchy order (skip ParameterEntry - it only contains Name)
    var groupViewModels = new List<PropertyGroupViewModel>();
    foreach (var type in hierarchy)
    {
      // Skip ParameterEntry group as Name is already shown in entity header
      if (type.Name == "ParameterEntry")
        continue;

      if (propertiesByType.TryGetValue(type, out var properties) && properties.Any())
      {
        var groupName = FormatTypeName(type);
        groupViewModels.Add(new PropertyGroupViewModel
        {
          GroupName = groupName,
          IsExpanded = type == researchType, // Expand only the most derived type
          Properties = new ObservableCollection<PropertyEditorViewModel>(properties)
        });
      }
    }

    return groupViewModels;
  }

  private void LoadEntity()
  {
    // Clear existing groups to prevent duplication
    PropertyGroups.Clear();
    ValidationErrors.Clear();
    
    _entityName = _currentEntity!.DisplayName;
    this.RaisePropertyChanged(nameof(EntityName));
    this.RaisePropertyChanged(nameof(EntityType));
    this.RaisePropertyChanged(nameof(ClassType));

    // Create property editors with callback to mark entity as dirty
    var editors = _propertyEditorFactory.CreateEditorsForEntity(
      _currentEntity.Entity, 
      () => _currentEntity.MarkDirty(),
      _parFile,
      _navigateToResearch);
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

  private void LoadResearch()
  {
    // Clear existing groups to prevent duplication
    PropertyGroups.Clear();
    ValidationErrors.Clear();
    
    _entityName = _currentResearch!.Name;
    this.RaisePropertyChanged(nameof(EntityName));
    this.RaisePropertyChanged(nameof(EntityType));
    this.RaisePropertyChanged(nameof(ClassType));

    // Create property editors for Research
    var editors = _propertyEditorFactory.CreateEditorsForResearch(_currentResearch, null, _parFile, _navigateToResearch);
    var groupedEditors = GroupResearchProperties(editors);

    foreach (var group in groupedEditors)
    {
      PropertyGroups.Add(group);
    }

    // Validate
    Validate();

    _logger.LogInformation("Loaded research details for '{Name}' ({Type}) with {PropertyCount} properties",
      _currentResearch.Name, _currentResearch.Type, editors.Count());
  }
  
  private IEnumerable<PropertyGroupViewModel> GroupProperties(IEnumerable<PropertyEditorViewModel> editors)
  {
    if (_currentEntity == null)
      return Enumerable.Empty<PropertyGroupViewModel>();

    // Group properties by declaring type using reflection
    var entityType = _currentEntity.Entity.GetType();
    var propertiesByType = new Dictionary<Type, List<PropertyEditorViewModel>>();

    foreach (var editor in editors)
    {
      // Find which type in the hierarchy declares this property
      var declaringType = FindDeclaringType(entityType, editor.PropertyName);
      
      if (declaringType != null)
      {
        if (!propertiesByType.ContainsKey(declaringType))
        {
          propertiesByType[declaringType] = new List<PropertyEditorViewModel>();
        }
        propertiesByType[declaringType].Add(editor);
      }
    }

    // Build inheritance hierarchy from base to derived
    var hierarchy = new List<Type>();
    var currentType = entityType;
    while (currentType != null && currentType != typeof(object))
    {
      hierarchy.Add(currentType);
      currentType = currentType.BaseType;
    }
    hierarchy.Reverse(); // Now ordered from base (Entity) to derived (Vehicle, etc.)

    // Create groups in hierarchy order (skip ParameterEntry - it only contains Name)
    var groupViewModels = new List<PropertyGroupViewModel>();
    foreach (var type in hierarchy)
    {
      // Skip ParameterEntry group as Name is already shown in entity header
      if (type.Name == "ParameterEntry")
        continue;

      if (propertiesByType.TryGetValue(type, out var properties) && properties.Any())
      {
        var groupName = FormatTypeName(type);
        groupViewModels.Add(new PropertyGroupViewModel
        {
          GroupName = groupName,
          IsExpanded = type == entityType, // Expand only the most derived type
          Properties = new ObservableCollection<PropertyEditorViewModel>(properties)
        });
      }
    }

    return groupViewModels;
  }

  private Type? FindDeclaringType(Type type, string propertyName)
  {
    var property = type.GetProperty(propertyName);
    return property?.DeclaringType;
  }

  private string FormatTypeName(Type type)
  {
    var name = type.Name;
    
    // Remove "Entity" suffix if present
    if (name.EndsWith("Entity") && !name.Equals("Entity"))
    {
      name = name.Substring(0, name.Length - 6);
    }
    
    // Convert PascalCase to "Pascal Case"
    if (string.IsNullOrEmpty(name))
      return name;
      
    var result = string.Concat(name.Select((c, i) =>
      i > 0 && char.IsUpper(c) && !char.IsUpper(name[i - 1]) ? " " + c : c.ToString()));
    
    return result;
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

  /// <summary>
  /// Gets the number of properties in this group.
  /// </summary>
  public int PropertyCount => Properties?.Count ?? 0;

  /// <summary>
  /// Gets the number of invalid properties in this group.
  /// </summary>
  public int ErrorCount => Properties?.Count(p => !p.IsValid) ?? 0;

  /// <summary>
  /// Gets whether this group has any invalid properties.
  /// </summary>
  public bool HasErrors => ErrorCount > 0;
}
