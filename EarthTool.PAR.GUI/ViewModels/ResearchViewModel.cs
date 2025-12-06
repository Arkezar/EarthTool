using EarthTool.PAR.Enums;
using EarthTool.PAR.Models;
using System.Collections.ObjectModel;

namespace EarthTool.PAR.GUI.ViewModels;

/// <summary>
/// ViewModel for a research entry (leaf node in research tree).
/// Example: "🔬 Advanced Chassis"
/// </summary>
public class ResearchViewModel : TreeNodeViewModelBase
{
  private readonly Research _research;

  public ResearchViewModel(Research research)
  {
    _research = research ?? throw new System.ArgumentNullException(nameof(research));
  }

  /// <summary>
  /// Gets the underlying research model.
  /// </summary>
  public Research Research => _research;

  /// <summary>
  /// Gets the research name.
  /// </summary>
  public string Name => _research.Name;

  /// <summary>
  /// Gets the research faction.
  /// </summary>
  public Faction Faction => _research.Faction;

  /// <summary>
  /// Gets the research type.
  /// </summary>
  public ResearchType Type => _research.Type;

  /// <summary>
  /// Gets the research ID.
  /// </summary>
  public int Id => _research.Id;

  public override string Icon => "🔬";

  public override string DisplayName => Name;

  public override ObservableCollection<TreeNodeViewModelBase>? Children => null;

  public override int ChildCount => 0;
}
