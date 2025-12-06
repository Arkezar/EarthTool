using EarthTool.PAR.Enums;
using System.Collections.ObjectModel;

namespace EarthTool.PAR.GUI.ViewModels;

/// <summary>
/// Tree node representing a ResearchType (second level in research hierarchy).
/// Example: "📂 Chassis (3 research)"
/// </summary>
public class ResearchTypeNodeViewModel : TreeNodeViewModelBase
{
  private readonly ResearchType _researchType;
  private readonly ObservableCollection<TreeNodeViewModelBase> _children;

  public ResearchTypeNodeViewModel(ResearchType researchType)
  {
    _researchType = researchType;
    _children = new ObservableCollection<TreeNodeViewModelBase>();
  }

  /// <summary>
  /// The research type this node represents
  /// </summary>
  public ResearchType ResearchType => _researchType;

  /// <summary>
  /// Collection of Research nodes
  /// </summary>
  public ObservableCollection<ResearchViewModel> ResearchItems { get; }
    = new ObservableCollection<ResearchViewModel>();

  public override string Icon => "📂";

  public override string DisplayName => $"{_researchType} ({ResearchItems.Count} research)";

  public override ObservableCollection<TreeNodeViewModelBase>? Children
  {
    get
    {
      // Sync with ResearchItems
      _children.Clear();
      foreach (var research in ResearchItems)
        _children.Add(research);
      return _children;
    }
  }

  public override int ChildCount => ResearchItems.Count;
}
