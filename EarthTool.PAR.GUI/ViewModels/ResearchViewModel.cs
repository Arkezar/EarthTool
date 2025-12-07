using EarthTool.PAR.Enums;
using EarthTool.PAR.GUI.Models;
using EarthTool.PAR.Models;
using ReactiveUI;
using System;
using System.Collections.ObjectModel;

namespace EarthTool.PAR.GUI.ViewModels;

/// <summary>
/// ViewModel for a research entry (leaf node in research tree).
/// Example: "🔬 Advanced Chassis"
/// </summary>
public class ResearchViewModel : TreeNodeViewModelBase
{
  private readonly EditableResearch _editableResearch;

  public ResearchViewModel(Research research)
  {
    if (research == null)
      throw new System.ArgumentNullException(nameof(research));
      
    _editableResearch = new EditableResearch(research);
    
    // Subscribe to dirty changes to trigger UI updates
    _editableResearch.WhenAnyValue(x => x.IsDirty)
      .Subscribe(isDirty => this.RaisePropertyChanged(nameof(IsDirty)));
  }

  /// <summary>
  /// Gets the underlying research model.
  /// </summary>
  public Research Research => _editableResearch.Research;
  
  /// <summary>
  /// Gets the editable research wrapper.
  /// </summary>
  public EditableResearch EditableResearch => _editableResearch;
  
  /// <summary>
  /// Gets whether the research has unsaved changes.
  /// </summary>
  public bool IsDirty => _editableResearch.IsDirty;

  /// <summary>
  /// Gets the research name.
  /// </summary>
  public string Name => _editableResearch.Research.Name;

  /// <summary>
  /// Gets the research faction.
  /// </summary>
  public Faction Faction => _editableResearch.Research.Faction;

  /// <summary>
  /// Gets the research type.
  /// </summary>
  public ResearchType Type => _editableResearch.Research.Type;

  /// <summary>
  /// Gets the research ID.
  /// </summary>
  public int Id => _editableResearch.Research.Id;

  public override string Icon => "🔬";

  public override string DisplayName => Name;

  public override ObservableCollection<TreeNodeViewModelBase>? Children => null;

  public override int ChildCount => 0;
}
